namespace SearchService.Models;

public record SearchResponseDto(long Total, int Page, int PageSize, IReadOnlyCollection<SearchResultDto> Results);
