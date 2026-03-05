using Anders.Warehouse.Web.Domain;

namespace Anders.Warehouse.Web.Interfaces;

public interface ITenantProvider
{
    Guid GetTenantId();
    string GetUserId();
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(string container, string fileName, Stream stream, CancellationToken ct = default);
}

public interface ISecretStore
{
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);
}

public record ParsedPurchaseDto(string SupplierName, DateTime InvoiceDate, decimal? TotalPrice, List<ParsedPurchaseLineDto> Lines);
public record ParsedPurchaseLineDto(string ProductName, decimal UnitPrice, decimal Quantity, decimal Confidence);

public interface IPdfParserPipeline
{
    Task<ParsedPurchaseDto> ParsePurchaseAsync(Stream pdfStream, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task RegisterPurchaseInAsync(PurchaseInvoice invoice, CancellationToken ct = default);
    Task RegisterSaleOutAsync(Guid productId, decimal quantity, DateTime date, string note, CancellationToken ct = default);
}

public interface IAuditService
{
    Task LogAsync(string entity, string entityId, string action, object payload, CancellationToken ct = default);
}

public interface IAgreementValidationService
{
    Task<DeviationReport> ValidateInvoiceAsync(PurchaseInvoice invoice, CancellationToken ct = default);
}

public interface IProductImageService
{
    Task<string?> FetchAndStoreProductImageAsync(Product product, CancellationToken ct = default);
}

public record PriceResult(decimal Price, string Currency);
public interface IPriceSource
{
    string Type { get; }
    Task<PriceResult?> TryGetPriceAsync(Product product, PriceSourceConfig source, CancellationToken ct = default);
}

public interface IPriceMonitorService
{
    Task ExecuteWeeklyAsync(CancellationToken ct = default);
}

public interface IShopifyClient { /* TODO: Implement Shopify integration */ }
public interface IMagentoClient { /* TODO: Implement Magento integration */ }
