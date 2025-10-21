using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using Records.Configuration;
using SearchService.Models;
using SearchService.Services;

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

builder.Services.AddScoped<IRecordSearchService, OpenSearchRecordSearchService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Search API",
        Version = "v1",
        Description = "REST API для полнотекстового поиска по агрегированным данным"
    });
});

builder.Services.AddCors(policy =>
{
    policy.AddPolicy("AllowAll", p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.UseStatusCodePages();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/search", async Task<IResult> (
    string? query,
    int? page,
    int? pageSize,
    string? dataset,
    string? sort,
    IRecordSearchService searchService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await searchService.SearchAsync(query ?? string.Empty, page ?? 1, pageSize ?? 25, dataset, sort, cancellationToken);
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("SearchRecords")
.Produces<SearchResponseDto>();

app.MapGet("/api/datasets", async Task<IResult> (IRecordSearchService searchService, CancellationToken cancellationToken) =>
{
    try
    {
        var datasets = await searchService.GetDatasetsAsync(cancellationToken);
        return Results.Ok(datasets);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("ListDatasets")
.Produces<IReadOnlyCollection<string>>();

app.Run();
