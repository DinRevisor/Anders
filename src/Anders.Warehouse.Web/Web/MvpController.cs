using Anders.Warehouse.Web.Data;
using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Anders.Warehouse.Web.Web;

[ApiController]
[Route("api/mvp")]
public class MvpController(
    ApplicationDbContext db,
    ITenantProvider tenantProvider,
    IPdfParserPipeline pdfParser,
    IBlobStorageService blobStorage,
    IInventoryService inventoryService,
    IAgreementValidationService agreementValidationService,
    IProductImageService productImageService,
    IPriceMonitorService priceMonitorService) : ControllerBase
{
    [HttpPost("tenant")]
    public async Task<IActionResult> CreateTenant([FromBody] string name)
    {
        var t = new Tenant { Name = name };
        db.Tenants.Add(t);
        await db.SaveChangesAsync();
        return Ok(t);
    }

    [HttpPost("purchase/upload")]
    public async Task<IActionResult> UploadPurchase(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var parsed = await pdfParser.ParsePurchaseAsync(stream, ct);
        var blobUrl = await blobStorage.UploadAsync("purchase-pdf", file.FileName, file.OpenReadStream(), ct);

        var invoice = new PurchaseInvoice
        {
            TenantId = tenantProvider.GetTenantId(),
            SupplierName = parsed.SupplierName,
            InvoiceDate = parsed.InvoiceDate,
            PdfBlobUrl = blobUrl,
            TotalPrice = parsed.TotalPrice,
            CreatedBy = tenantProvider.GetUserId(),
            Lines = parsed.Lines.Select(l => new PurchaseLine
            {
                ProductName = l.ProductName,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity,
                ParsedConfidence = l.Confidence
            }).ToList()
        };
        db.PurchaseInvoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return Ok(invoice);
    }

    [HttpPost("purchase/{invoiceId:guid}/approve")]
    public async Task<IActionResult> ApprovePurchase(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.PurchaseInvoices.Include(x => x.Lines).FirstAsync(x => x.Id == invoiceId, ct);
        foreach (var line in invoice.Lines)
        {
            var product = await db.Products.FirstOrDefaultAsync(x => x.Name == line.ProductName && x.TenantId == invoice.TenantId, ct);
            if (product is null)
            {
                product = new Product { TenantId = invoice.TenantId, Name = line.ProductName, SupplierName = invoice.SupplierName };
                db.Products.Add(product);
            }
            await db.SaveChangesAsync(ct);
            product.ImageUrl = await productImageService.FetchAndStoreProductImageAsync(product, ct);
            line.ProductId = product.Id;
        }

        await db.SaveChangesAsync(ct);
        await inventoryService.RegisterPurchaseInAsync(invoice, ct);
        var report = await agreementValidationService.ValidateInvoiceAsync(invoice, ct);
        return Ok(new { invoice.Id, report.Summary });
    }

    [HttpPost("sale")]
    public async Task<IActionResult> RegisterSale(Guid productId, decimal quantity, DateTime date, string note, CancellationToken ct)
    {
        await inventoryService.RegisterSaleOutAsync(productId, quantity, date, note, ct);
        return Ok();
    }

    [HttpPost("price-monitor/run")]
    public async Task<IActionResult> RunMonitor(CancellationToken ct)
    {
        await priceMonitorService.ExecuteWeeklyAsync(ct);
        return Ok();
    }
}
