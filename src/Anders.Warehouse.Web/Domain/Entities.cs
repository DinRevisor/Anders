using Microsoft.AspNetCore.Identity;

namespace Anders.Warehouse.Web.Domain;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}

public interface ITenantEntity { Guid TenantId { get; set; } }

public class Product : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class InventoryBalance : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum InventoryDirection { IN, OUT }

public class InventoryTransaction : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public InventoryDirection Direction { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}

public class PurchaseInvoice : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string PdfBlobUrl { get; set; } = string.Empty;
    public decimal? TotalPrice { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<PurchaseLine> Lines { get; set; } = [];
}

public class PurchaseLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal ParsedConfidence { get; set; }
    public Guid? ProductId { get; set; }
}

public class Agreement : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string PdfBlobUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<AgreementRule> Rules { get; set; } = [];
}

public class AgreementRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgreementId { get; set; }
    public string ProductPattern { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public decimal? QuantityThreshold { get; set; }
    public decimal? DiscountPercentAtThreshold { get; set; }
}

public class DeviationReport : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MonitorDirection { UP, DOWN, BOTH }

public class PriceMonitor : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public decimal ThresholdPercent { get; set; }
    public MonitorDirection Direction { get; set; }
    public string Frequency { get; set; } = "Weekly";
    public List<PriceSourceConfig> Sources { get; set; } = [];
}

public class PriceSourceConfig : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MonitorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceType { get; set; } = "Web";
    public string? Url { get; set; }
    public string? SecretKeyRef { get; set; }
    public int Priority { get; set; }
}

public class PriceSnapshot : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SourceId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "DKK";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class InAppNotification : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditEvent : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string PayloadJson { get; set; } = string.Empty;
}
