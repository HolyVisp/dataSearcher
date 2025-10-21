using System.ComponentModel.DataAnnotations;

namespace DataIngestionService.Models;

public class ManualRecordRequest
{
    [Required]
    public Dictionary<string, object> Fields { get; set; } = new();
}
