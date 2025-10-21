using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using DataIngestionService.Models;
using DataIngestionService.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using Records.Configuration;
using Records.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenSearchOptions>(builder.Configuration.GetSection(OpenSearchOptions.SectionName));
builder.Services.AddSingleton<IOpenSearchClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<OpenSearchOptions>>().Value;
    var pool = new SingleNodeConnectionPool(new Uri(options.Uri));
    var settings = new ConnectionSettings(pool)
        .DefaultIndex(options.IndexName)
        .EnableApiVersioningHeader()
        .DefaultFieldNameInferrer(p => p);

    if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
    {
        settings = settings.BasicAuthentication(options.Username, options.Password);
    }

    if (options.DisableCertificateValidation)
    {
        settings.ServerCertificateValidationCallback((_, _, _, _) => true);
    }

    settings.DisableDirectStreaming();

    return new OpenSearchClient(settings);
});

builder.Services.AddScoped<IRecordIngestionService, OpenSearchRecordIngestionService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Data ingestion API",
        Version = "v1",
        Description = "API для загрузки и индексирования произвольных таблиц в OpenSearch"
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddCors(policy =>
{
    policy.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

var app = builder.Build();

await EnsureIndexAsync(app.Services);

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.UseStatusCodePages();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/datasets/{dataset}/records", async Task<IResult> (string dataset, ManualRecordRequest request, IRecordIngestionService service, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(dataset))
    {
        return Results.BadRequest("Dataset name is required");
    }

    if (request.Fields.Count == 0)
    {
        return Results.BadRequest("At least one field is required");
    }

    var record = BuildRecord(dataset, request.Fields);
    var response = await service.IndexManyAsync(new[] { record }, cancellationToken);

    return Results.Ok(new BulkIngestionSummary(1, response.Indexed, response.Failed, response.Errors));
})
.WithName("AddManualRecord")
.Produces<BulkIngestionSummary>();

app.MapPost("/api/datasets/{dataset}/upload", async Task<IResult> (string dataset, IFormFile file, IRecordIngestionService service, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(dataset))
    {
        return Results.BadRequest("Dataset name is required");
    }

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("Файл не найден или пуст");
    }

    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var records = extension switch
    {
        ".csv" => ParseCsv(dataset, file.OpenReadStream()),
        ".json" => ParseJson(dataset, file.OpenReadStream()),
        _ => throw new InvalidOperationException("Поддерживаются только CSV и JSON файлы")
    };

    var response = await service.IndexManyAsync(records, cancellationToken);

    return Results.Ok(new BulkIngestionSummary(response.Indexed + response.Failed, response.Indexed, response.Failed, response.Errors));
})
.WithName("UploadDataset")
.Produces<BulkIngestionSummary>();

app.Run();

static async Task EnsureIndexAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var ingestionService = scope.ServiceProvider.GetRequiredService<IRecordIngestionService>();
    await ingestionService.EnsureIndexAsync(CancellationToken.None);
}

static RecordDocument BuildRecord(string dataset, Dictionary<string, object> fields)
{
    var cleaned = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    foreach (var kvp in fields)
    {
        if (kvp.Value is JsonElement jsonElement)
        {
            cleaned[kvp.Key] = jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Number => jsonElement.TryGetInt64(out var longValue) ? longValue : jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => jsonElement.EnumerateArray().Select(e => e.ToString()).ToArray(),
                _ => jsonElement.ToString()
            };
        }
        else
        {
            cleaned[kvp.Key] = kvp.Value;
        }
    }

    var tokens = cleaned.Values
        .Select(value =>
        {
            if (value is null)
            {
                return string.Empty;
            }

            if (value is string stringValue)
            {
                return stringValue;
            }

            if (value is System.Collections.IEnumerable enumerable)
            {
                var items = enumerable
                    .Cast<object?>()
                    .Select(item => item?.ToString())
                    .Where(item => !string.IsNullOrWhiteSpace(item));
                return string.Join(' ', items);
            }

            return value.ToString() ?? string.Empty;
        })
        .Where(part => !string.IsNullOrWhiteSpace(part));

    var record = new RecordDocument
    {
        Dataset = dataset,
        Fields = cleaned,
        SearchVector = string.Join(' ', tokens)
    };

    return record;
}

static IEnumerable<RecordDocument> ParseCsv(string dataset, Stream stream)
{
    using var reader = new StreamReader(stream);
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        DetectDelimiter = true,
        BadDataFound = null,
        MissingFieldFound = null,
        TrimOptions = TrimOptions.Trim,
        IgnoreBlankLines = true
    };

    using var csv = new CsvReader(reader, config);
    if (!csv.Read())
    {
        yield break;
    }

    csv.ReadHeader();
    var headers = csv.HeaderRecord ?? Array.Empty<string>();

    while (csv.Read())
    {
        var record = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            record[header] = csv.GetField(header) ?? string.Empty;
        }

        yield return BuildRecord(dataset, record);
    }
}

static IEnumerable<RecordDocument> ParseJson(string dataset, Stream stream)
{
    using var document = JsonDocument.Parse(stream);
    if (document.RootElement.ValueKind == JsonValueKind.Array)
    {
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var dictionary = element.EnumerateObject().ToDictionary(p => p.Name, p => (object)p.Value);
            yield return BuildRecord(dataset, dictionary);
        }
    }
    else if (document.RootElement.ValueKind == JsonValueKind.Object)
    {
        var dictionary = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object)p.Value);
        yield return BuildRecord(dataset, dictionary);
    }
    else
    {
        throw new InvalidOperationException("JSON должен содержать объект или массив объектов");
    }
}
