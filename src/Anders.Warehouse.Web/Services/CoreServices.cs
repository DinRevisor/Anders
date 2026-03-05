using Anders.Warehouse.Web.Data;
using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Anders.Warehouse.Web.Services;

public class HttpTenantProvider : ITenantProvider
{
    public Guid GetTenantId() => Guid.Empty; // TODO derive from claims/session
    public string GetUserId() => "system";
}

public class AuditService(ApplicationDbContext db, ITenantProvider tenantProvider) : IAuditService
{
    public async Task LogAsync(string entity, string entityId, string action, object payload, CancellationToken ct = default)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantProvider.GetTenantId(),
            EntityName = entity,
            EntityId = entityId,
            Action = action,
            ChangedBy = tenantProvider.GetUserId(),
            PayloadJson = JsonSerializer.Serialize(payload)
        });
        await db.SaveChangesAsync(ct);
    }
}

public class InventoryService(ApplicationDbContext db, ITenantProvider tenantProvider, IAuditService auditService) : IInventoryService
{
    public async Task RegisterPurchaseInAsync(PurchaseInvoice invoice, CancellationToken ct = default)
    {
        foreach (var line in invoice.Lines)
        {
            if (line.ProductId is null) continue;
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                TenantId = tenantProvider.GetTenantId(),
                ProductId = line.ProductId.Value,
                Direction = InventoryDirection.IN,
                Quantity = line.Quantity,
                Reason = "PurchaseInvoice",
                Reference = invoice.Id.ToString()
            });
            var balance = await db.InventoryBalances.FirstOrDefaultAsync(x => x.ProductId == line.ProductId && x.TenantId == invoice.TenantId, ct);
            if (balance is null)
            {
                balance = new InventoryBalance { TenantId = invoice.TenantId, ProductId = line.ProductId.Value, Quantity = 0 };
                db.InventoryBalances.Add(balance);
            }
            balance.Quantity += line.Quantity;
            balance.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(nameof(PurchaseInvoice), invoice.Id.ToString(), "InventoryIN", invoice, ct);
    }

    public async Task RegisterSaleOutAsync(Guid productId, decimal quantity, DateTime date, string note, CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetTenantId();
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TenantId = tenantId,
            ProductId = productId,
            Direction = InventoryDirection.OUT,
            Quantity = quantity,
            Reason = "Sold",
            Note = note,
            TransactionDate = date
        });
        var balance = await db.InventoryBalances.FirstAsync(x => x.ProductId == productId && x.TenantId == tenantId, ct);
        balance.Quantity -= quantity;
        balance.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(nameof(Product), productId.ToString(), "InventoryOUT", new { quantity, date, note }, ct);
    }
}

public class AgreementValidationService(ApplicationDbContext db, ITenantProvider tenantProvider) : IAgreementValidationService
{
    public async Task<DeviationReport> ValidateInvoiceAsync(PurchaseInvoice invoice, CancellationToken ct = default)
    {
        var rules = await db.AgreementRules.Where(r => db.Agreements.Any(a => a.Id == r.AgreementId && a.TenantId == tenantProvider.GetTenantId() && a.SupplierName == invoice.SupplierName)).ToListAsync(ct);
        var deviations = new List<string>();
        foreach (var line in invoice.Lines)
        {
            var rule = rules.FirstOrDefault(r => line.ProductName.Contains(r.ProductPattern, StringComparison.OrdinalIgnoreCase));
            if (rule == null) continue;
            var expected = line.UnitPrice * (1 - rule.DiscountPercent / 100m);
            if (line.Quantity >= rule.QuantityThreshold && rule.DiscountPercentAtThreshold.HasValue)
                expected = line.UnitPrice * (1 - rule.DiscountPercentAtThreshold.Value / 100m);
            if (line.UnitPrice > expected * 1.02m) deviations.Add($"{line.ProductName}: expected <= {expected}");
        }

        var report = new DeviationReport
        {
            TenantId = invoice.TenantId,
            InvoiceId = invoice.Id,
            Summary = deviations.Count == 0 ? "No deviations" : string.Join(" | ", deviations)
        };
        db.DeviationReports.Add(report);
        await db.SaveChangesAsync(ct);
        return report;
    }
}
