using Records.Models;

namespace DataIngestionService.Services;

public interface IRecordIngestionService
{
    Task EnsureIndexAsync(CancellationToken cancellationToken);

    Task<BulkResponse> IndexManyAsync(IEnumerable<RecordDocument> records, CancellationToken cancellationToken);
}

public record BulkResponse(int Indexed, int Failed, IReadOnlyCollection<string> Errors);
