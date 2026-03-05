using Anders.Warehouse.Web.Data;
using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Anders.Warehouse.Web.Pages;

public class FakturaerModel(ApplicationDbContext db, ITenantProvider tenantProvider) : PageModel
{
    public List<PurchaseInvoice> Invoices { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            tenantId = await db.Tenants.OrderBy(x => x.CreatedAt).Select(x => x.Id).FirstOrDefaultAsync(ct);
        }

        Invoices = await db.PurchaseInvoices
            .Where(x => x.TenantId == tenantId)
            .Include(x => x.Lines)
            .OrderByDescending(x => x.InvoiceDate)
            .Take(50)
            .ToListAsync(ct);
    }
}
