namespace SearchService.Models;

public record SearchResultDto(string Id, string Dataset, string Index, IReadOnlyDictionary<string, object> Fields, double? Score, DateTimeOffset CreatedAt);
