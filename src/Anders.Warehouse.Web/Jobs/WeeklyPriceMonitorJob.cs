using Anders.Warehouse.Web.Interfaces;

namespace Anders.Warehouse.Web.Jobs;

public class WeeklyPriceMonitorJob(IPriceMonitorService service)
{
    public Task RunAsync() => service.ExecuteWeeklyAsync();
}
