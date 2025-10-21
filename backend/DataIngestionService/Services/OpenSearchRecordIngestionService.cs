using Microsoft.Extensions.Options;
using OpenSearch.Client;
using Records.Configuration;
using Records.Models;

namespace DataIngestionService.Services;

public class OpenSearchRecordIngestionService : IRecordIngestionService
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchOptions _options;
    private const int BatchSize = 1000;

    public OpenSearchRecordIngestionService(IOpenSearchClient client, IOptions<OpenSearchOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        var exists = await _client.Indices.ExistsAsync(_options.IndexName, ct: cancellationToken);
        if (exists.Exists)
        {
            return;
        }

        var response = await _client.Indices.CreateAsync(_options.IndexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(0))
            .Map<RecordDocument>(m => m
                .AutoMap()
                .DynamicTemplates(dt => dt
                    .DynamicTemplate("fields_strings", t => t
                        .PathMatch("fields.*")
                        .Mapping(ma => ma.Text(t => t
                            .Fields(f => f.Keyword(k => k.Name("keyword")))))))
                .Properties(ps => ps
                    .Keyword(k => k.Name(p => p.Dataset))
                    .Date(d => d.Name(p => p.CreatedAt))
                    .Text(t => t.Name(p => p.SearchVector))
                    .Object<object>(o => o.Name(p => p.Fields).Dynamic()))), cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to create index '{_options.IndexName}': {response.ServerError?.Error.Reason}");
        }
    }

    public async Task<BulkResponse> IndexManyAsync(IEnumerable<RecordDocument> records, CancellationToken cancellationToken)
    {
        var buffer = new List<RecordDocument>(BatchSize);
        var indexed = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var record in records)
        {
            buffer.Add(record);
            if (buffer.Count >= BatchSize)
            {
                var result = await IndexBatchAsync(buffer, cancellationToken);
                indexed += result.Indexed;
                failed += result.Failed;
                errors.AddRange(result.Errors);
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            var result = await IndexBatchAsync(buffer, cancellationToken);
            indexed += result.Indexed;
            failed += result.Failed;
            errors.AddRange(result.Errors);
        }

        return new BulkResponse(indexed, failed, errors);
    }

    private async Task<BulkResponse> IndexBatchAsync(List<RecordDocument> records, CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return new BulkResponse(0, 0, Array.Empty<string>());
        }

        var response = await _client.BulkAsync(b =>
        {
            b.Index(_options.IndexName);
            foreach (var record in records)
            {
                b.Index<RecordDocument>(i => i.Document(record));
            }

            return b;
        }, cancellationToken);

        if (!response.IsValid)
        {
            var errorMessage = response.ServerError?.ToString() ?? "Unknown error";
            return new BulkResponse(0, records.Count, new[] { errorMessage });
        }

        var failedItems = response.ItemsWithErrors?.ToList() ?? new List<OpenSearch.Net.BulkResponseItemBase>();
        var failedCount = failedItems.Count;
        var errorMessages = failedItems.Select(i => i.Error?.Reason ?? "Unknown failure").ToList();

        return new BulkResponse(records.Count - failedCount, failedCount, errorMessages);
    }
}
