using Anders.Warehouse.Web.Data;
using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Interfaces;
using Anders.Warehouse.Web.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace Anders.Warehouse.Web.Infrastructure;

public class PdfParserPipeline : IPdfParserPipeline
{
    public async Task<ParsedPurchaseDto> ParsePurchaseAsync(Stream pdfStream, CancellationToken ct = default)
    {
        using var mem = new MemoryStream();
        await pdfStream.CopyToAsync(mem, ct);
        mem.Position = 0;
        string text;
        using (var doc = PdfDocument.Open(mem))
        {
            text = string.Join("\n", doc.GetPages().Select(p => p.Text));
        }

        if (text.Length < 50)
        {
            text = "OCR_FALLBACK: simulated extraction"; // TODO integrate Tesseract
        }

        var supplier = text.Contains("leverand", StringComparison.OrdinalIgnoreCase) ? "Parsed Supplier" : "Unknown Supplier";
        var lines = text.Split('\n').Where(x => x.Contains("kr", StringComparison.OrdinalIgnoreCase)).Take(5)
            .Select(x => new ParsedPurchaseLineDto(x.Trim(), 100m, 1, 0.55m)).ToList();
        if (lines.Count == 0) lines.Add(new ParsedPurchaseLineDto("Manual Product", 100m, 1, 0.2m));

        return new ParsedPurchaseDto(supplier, DateTime.UtcNow.Date, null, lines);
    }
}

public class BlobStorageService(IOptions<StorageOptions> options) : IBlobStorageService
{
    public Task<string> UploadAsync(string container, string fileName, Stream stream, CancellationToken ct = default)
    {
        var baseUrl = options.Value.BlobBaseUrl?.TrimEnd('/') ?? "https://local-blob.invalid";
        return Task.FromResult($"{baseUrl}/{container}/{fileName}");
    }
}

public class UserSecretsSecretStore(IConfiguration configuration) : ISecretStore
{
    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
        => Task.FromResult(configuration[$"Secrets:{key}"]);
}

public class GoogleProductImageService(IBlobStorageService blobStorageService, IOptions<GoogleSearchOptions> options) : IProductImageService
{
    public async Task<string?> FetchAndStoreProductImageAsync(Product product, CancellationToken ct = default)
    {
        var query = $"{product.Name} {product.SupplierName}";
        var fakeImage = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var blob = await blobStorageService.UploadAsync("product-images", $"{product.Id}.jpg", fakeImage, ct);
        var minW = options.Value.MinWidth;
        var minH = options.Value.MinHeight;
        return $"{blob}?src=google&q={Uri.EscapeDataString(query)}&minW={minW}&minH={minH}";
    }
}

public class ApiPriceSource : IPriceSource
{
    public string Type => "Api";
    public Task<PriceResult?> TryGetPriceAsync(Product product, PriceSourceConfig source, CancellationToken ct = default)
        => Task.FromResult<PriceResult?>(new PriceResult(95m, "DKK"));
}

public class ScrapePriceSource(ISecretStore secretStore) : IPriceSource
{
    public string Type => "Scrape";
    public async Task<PriceResult?> TryGetPriceAsync(Product product, PriceSourceConfig source, CancellationToken ct = default)
    {
        _ = source.SecretKeyRef is null ? null : await secretStore.GetSecretAsync(source.SecretKeyRef, ct);
        // TODO Use Playwright for real scraping with retries/selectors.
        return new PriceResult(97m, "DKK");
    }
}

public class PriceMonitorService(ApplicationDbContext db, ITenantProvider tenantProvider, IEnumerable<IPriceSource> priceSources) : IPriceMonitorService
{
    public async Task ExecuteWeeklyAsync(CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetTenantId();
        var monitors = await db.PriceMonitors.Include(m => m.Sources).ToListAsync(ct);
        foreach (var monitor in monitors.Where(m => m.TenantId == tenantId))
        {
            var product = await db.Products.FirstAsync(x => x.Id == monitor.ProductId, ct);
            foreach (var src in monitor.Sources.OrderBy(x => x.Priority).Take(5))
            {
                var handler = priceSources.FirstOrDefault(x => x.Type.Equals(src.SourceType, StringComparison.OrdinalIgnoreCase));
                if (handler is null) continue;
                var price = await handler.TryGetPriceAsync(product, src, ct);
                if (price is null) continue;

                var snapshot = new PriceSnapshot { TenantId = tenantId, ProductId = product.Id, SourceId = src.Id, Price = price.Price, Currency = price.Currency };
                db.PriceSnapshots.Add(snapshot);

                var last = await db.PriceSnapshots.Where(x => x.ProductId == product.Id && x.SourceId == src.Id)
                    .OrderByDescending(x => x.Timestamp).Skip(1).FirstOrDefaultAsync(ct);
                if (last is null) continue;
                var diffPct = Math.Abs((snapshot.Price - last.Price) / last.Price * 100m);
                if (diffPct >= monitor.ThresholdPercent)
                {
                    db.Notifications.Add(new InAppNotification
                    {
                        TenantId = tenantId,
                        Title = $"Price alert for {product.Name}",
                        Body = $"Change {diffPct:F2}% from {last.Price} to {snapshot.Price}"
                    });
                }
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
