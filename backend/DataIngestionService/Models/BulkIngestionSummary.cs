namespace DataIngestionService.Models;

public record BulkIngestionSummary(int Processed, int Indexed, int Failed, IReadOnlyCollection<string> Errors);
