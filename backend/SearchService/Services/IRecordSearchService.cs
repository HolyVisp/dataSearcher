using SearchService.Models;

namespace SearchService.Services;

public interface IRecordSearchService
{
    Task<SearchResponseDto> SearchAsync(string query, int page, int pageSize, string? dataset, string? sort, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetDatasetsAsync(CancellationToken cancellationToken);
}
