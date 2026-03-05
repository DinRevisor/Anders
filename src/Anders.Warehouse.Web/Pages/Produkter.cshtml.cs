using Anders.Warehouse.Web.Data;
using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Anders.Warehouse.Web.Pages;

public class ProdukterModel(ApplicationDbContext db, ITenantProvider tenantProvider) : PageModel
{
    public List<Product> Products { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            tenantId = await db.Tenants.OrderBy(x => x.CreatedAt).Select(x => x.Id).FirstOrDefaultAsync(ct);
        }

        Products = await db.Products.Where(x => x.TenantId == tenantId).OrderBy(x => x.Name).ToListAsync(ct);
    }
}
