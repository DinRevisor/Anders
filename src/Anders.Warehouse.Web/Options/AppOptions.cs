namespace Anders.Warehouse.Web.Options;

public class GoogleSearchOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Cx { get; set; } = string.Empty;
    public int MinWidth { get; set; } = 400;
    public int MinHeight { get; set; } = 400;
}

public class StorageOptions
{
    public string BlobConnectionString { get; set; } = string.Empty;
    public string BlobBaseUrl { get; set; } = string.Empty;
}
