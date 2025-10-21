namespace Records.Configuration;

public class OpenSearchOptions
{
    public const string SectionName = "OpenSearch";

    public string Uri { get; set; } = "http://localhost:9200";

    public string IndexName { get; set; } = "records";

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool DisableCertificateValidation { get; set; } = false;
}
