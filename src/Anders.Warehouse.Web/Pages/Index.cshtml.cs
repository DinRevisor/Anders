using Anders.Warehouse.Web.Data;
using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Anders.Warehouse.Web.Pages;

public class IndexModel(
    ApplicationDbContext db,
    ITenantProvider tenantProvider,
    IPdfParserPipeline pdfParserPipeline,
    IBlobStorageService blobStorageService,
    IPriceMonitorService priceMonitorService,
    IInventoryService inventoryService) : PageModel
{
    [BindProperty] public IFormFile? UploadPdf { get; set; }
    [BindProperty] public string TenantName { get; set; } = string.Empty;
    [BindProperty] public Guid SaleProductId { get; set; }
    [BindProperty] public decimal SaleQuantity { get; set; } = 1;
    [BindProperty] public DateTime SaleDate { get; set; } = DateTime.UtcNow.Date;
    [BindProperty] public string SaleNote { get; set; } = string.Empty;

    [TempData] public string? StatusMessage { get; set; }

    public DashboardViewModel Dashboard { get; private set; } = new();
    public List<SelectListItem> ProductOptions { get; private set; } = [];

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostRunPriceMonitorAsync()
    {
        await priceMonitorService.ExecuteWeeklyAsync();
        StatusMessage = "Prisovervågning kørt.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateTenantAsync()
    {
        if (string.IsNullOrWhiteSpace(TenantName))
        {
            StatusMessage = "Virksomhedsnavn mangler.";
            return RedirectToPage();
        }

        db.Tenants.Add(new Tenant { Name = TenantName.Trim() });
        await db.SaveChangesAsync();
        StatusMessage = $"Virksomhed '{TenantName}' oprettet.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadPurchaseAsync(CancellationToken ct)
    {
        if (UploadPdf is null || UploadPdf.Length == 0)
        {
            StatusMessage = "Vælg en PDF fil først.";
            return RedirectToPage();
        }

        var tenantId = await ResolveTenantIdAsync(ct);
        await using var stream = UploadPdf.OpenReadStream();
        var parsed = await pdfParserPipeline.ParsePurchaseAsync(stream, ct);
        var blobUrl = await blobStorageService.UploadAsync("purchase-pdf", UploadPdf.FileName, UploadPdf.OpenReadStream(), ct);

        var invoice = new PurchaseInvoice
        {
            TenantId = tenantId,
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
        StatusMessage = $"Bilag uploadet med {invoice.Lines.Count} linjer.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRegisterSaleAsync(CancellationToken ct)
    {
        if (SaleProductId == Guid.Empty || SaleQuantity <= 0)
        {
            StatusMessage = "Vælg produkt og gyldigt antal.";
            return RedirectToPage();
        }

        await inventoryService.RegisterSaleOutAsync(SaleProductId, SaleQuantity, SaleDate, SaleNote, ct);
        StatusMessage = "Salg registreret.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdAsync(ct);
        var products = await db.Products.Where(x => x.TenantId == tenantId).OrderBy(x => x.Name).ToListAsync(ct);
        var balances = await db.InventoryBalances.Where(x => x.TenantId == tenantId).ToListAsync(ct);
        var transactions = await db.InventoryTransactions.Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.TransactionDate).Take(6).ToListAsync(ct);

        var maxQty = transactions.Select(x => x.Quantity).DefaultIfEmpty(1).Max();
        Dashboard = new DashboardViewModel
        {
            ProductCount = products.Count,
            InvoiceCount = await db.PurchaseInvoices.CountAsync(x => x.TenantId == tenantId, ct),
            NotificationCount = await db.Notifications.CountAsync(x => x.TenantId == tenantId && !x.IsRead, ct),
            TotalStock = balances.Sum(x => x.Quantity),
            ChartPoints = transactions.Select(t => new InventoryBar
            {
                Label = t.TransactionDate.ToString("dd/MM"),
                InHeight = t.Direction == InventoryDirection.IN ? Math.Max(8, (int)(t.Quantity / maxQty * 100)) : 8,
                OutHeight = t.Direction == InventoryDirection.OUT ? Math.Max(8, (int)(t.Quantity / maxQty * 100)) : 8
            }).Reverse().ToList(),
            Balances = balances.Join(products, b => b.ProductId, p => p.Id, (b, p) => new BalanceRow
            {
                ProductName = p.Name,
                Quantity = b.Quantity
            }).OrderByDescending(x => x.Quantity).Take(8).ToList()
        };

        ProductOptions = products.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
        ProductOptions.Insert(0, new SelectListItem("-- vælg produkt --", string.Empty));
    }

    private async Task<Guid> ResolveTenantIdAsync(CancellationToken ct)
    {
        var fromProvider = tenantProvider.GetTenantId();
        if (fromProvider != Guid.Empty) return fromProvider;

        var firstTenant = await db.Tenants.OrderBy(t => t.CreatedAt).Select(t => t.Id).FirstOrDefaultAsync(ct);
        return firstTenant;
    }

    public class DashboardViewModel
    {
        public int ProductCount { get; set; }
        public int InvoiceCount { get; set; }
        public int NotificationCount { get; set; }
        public decimal TotalStock { get; set; }
        public List<InventoryBar> ChartPoints { get; set; } = [];
        public List<BalanceRow> Balances { get; set; } = [];
    }

    public class InventoryBar
    {
        public string Label { get; set; } = string.Empty;
        public int InHeight { get; set; }
        public int OutHeight { get; set; }
    }

    public class BalanceRow
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }
}
