using Microsoft.Extensions.Options;
using OpenSearch.Client;
using Records.Configuration;
using Records.Models;
using SearchService.Models;

namespace SearchService.Services;

public class OpenSearchRecordSearchService : IRecordSearchService
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchOptions _options;

    public OpenSearchRecordSearchService(IOpenSearchClient client, IOptions<OpenSearchOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<SearchResponseDto> SearchAsync(string query, int page, int pageSize, string? dataset, string? sort, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var from = (page - 1) * pageSize;

        var descriptor = new SearchDescriptor<RecordDocument>()
            .Index(_options.IndexName)
            .From(from)
            .Size(pageSize)
            .Source(s => s.Includes(i => i
                .Fields(f => f.Dataset, f => f.Fields, f => f.CreatedAt, f => f.SearchVector)));

        descriptor = ApplyQuery(descriptor, query, dataset);
        descriptor = ApplySort(descriptor, sort);

        var response = await _client.SearchAsync<RecordDocument>(descriptor, cancellationToken);
        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Ошибка поиска: {response.ServerError?.Error.Reason ?? response.DebugInformation}");
        }

        var results = response.Hits.Select(hit =>
        {
            var source = hit.Source ?? new RecordDocument();
            var fields = source.Fields ?? new Dictionary<string, object>();
            return new SearchResultDto(
                hit.Id,
                source.Dataset,
                hit.Index,
                fields,
                hit.Score,
                source.CreatedAt
            );
        }).ToList();

        var total = response.HitsMetadata?.Total?.Value ?? results.Count;

        return new SearchResponseDto(total, page, pageSize, results);
    }

    public async Task<IReadOnlyCollection<string>> GetDatasetsAsync(CancellationToken cancellationToken)
    {
        var response = await _client.SearchAsync<RecordDocument>(s => s
            .Index(_options.IndexName)
            .Size(0)
            .Aggregations(a => a.Terms("datasets", t => t.Field(f => f.Dataset).Size(1000))), cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Ошибка получения данных: {response.ServerError?.Error.Reason ?? response.DebugInformation}");
        }

        var buckets = response.Aggregations.Terms("datasets")?.Buckets ?? Array.Empty<KeyedBucket<string>>();
        return buckets.Select(b => b.Key).OrderBy(b => b, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private SearchDescriptor<RecordDocument> ApplyQuery(SearchDescriptor<RecordDocument> descriptor, string query, string? dataset)
    {
        var mustQueries = new List<Func<QueryDescriptor<RecordDocument>, QueryContainer>>();
        var filters = new List<Func<QueryDescriptor<RecordDocument>, QueryContainer>>();

        if (string.IsNullOrWhiteSpace(query))
        {
            mustQueries.Add(m => m.MatchAll());
        }
        else
        {
            mustQueries.Add(m => m.SimpleQueryString(s => s
                .Query(query)
                .Fields(f => f
                    .Field(p => p.SearchVector)
                    .Field("fields.*"))));
        }

        if (!string.IsNullOrWhiteSpace(dataset))
        {
            filters.Add(f => f.Term(t => t.Field(p => p.Dataset).Value(dataset)));
        }

        descriptor = descriptor.Query(q => q.Bool(b =>
        {
            if (mustQueries.Count > 0)
            {
                b.Must(mustQueries.ToArray());
            }

            if (filters.Count > 0)
            {
                b.Filter(filters.ToArray());
            }

            return b;
        }));

        return descriptor;
    }

    private SearchDescriptor<RecordDocument> ApplySort(SearchDescriptor<RecordDocument> descriptor, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return descriptor.Sort(s => s
                .Descending(SortSpecialField.Score)
                .Descending(f => f.CreatedAt));
        }

        var parts = sort.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var field = parts.FirstOrDefault();
        var order = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? SortOrder.Descending
            : SortOrder.Ascending;

        return descriptor.Sort(s =>
        {
            if (string.Equals(field, "score", StringComparison.OrdinalIgnoreCase))
            {
                s.Field(f => f.Field(SortSpecialField.Score).Order(order));
            }
            else if (string.Equals(field, "createdAt", StringComparison.OrdinalIgnoreCase))
            {
                s.Field(f => f.Field(p => p.CreatedAt).Order(order));
            }
            else if (string.Equals(field, "dataset", StringComparison.OrdinalIgnoreCase))
            {
                s.Field(f => f.Field(p => p.Dataset).Order(order));
            }
            else if (!string.IsNullOrWhiteSpace(field))
            {
                s.Field(f => f.Field($"fields.{field}.keyword").Order(order).UnmappedType(FieldType.Keyword));
            }
            else
            {
                s.Field(f => f.Field(SortSpecialField.Score).Order(SortOrder.Descending));
            }

            return s;
        });
    }
}
