using Anders.Warehouse.Web.Data;
using Anders.Warehouse.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Anders.Warehouse.Web.Pages;

public class PrisovervaagningModel(ApplicationDbContext db, ITenantProvider tenantProvider, IPriceMonitorService monitorService) : PageModel
{
    public List<MonitorRow> Monitors { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await monitorService.ExecuteWeeklyAsync(ct);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var tenantId = tenantProvider.GetTenantId();
        if (tenantId == Guid.Empty)
        {
            tenantId = await db.Tenants.OrderBy(x => x.CreatedAt).Select(x => x.Id).FirstOrDefaultAsync(ct);
        }

        Monitors = await db.PriceMonitors
            .Where(x => x.TenantId == tenantId)
            .Join(db.Products, m => m.ProductId, p => p.Id,
                (m, p) => new MonitorRow
                {
                    ProductName = p.Name,
                    ThresholdPercent = m.ThresholdPercent,
                    Direction = m.Direction.ToString(),
                    Frequency = m.Frequency
                })
            .OrderBy(x => x.ProductName)
            .ToListAsync(ct);
    }

    public class MonitorRow
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal ThresholdPercent { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
    }
}
