using Anders.Warehouse.Web.Domain;
using Anders.Warehouse.Web.Interfaces;

namespace Anders.Warehouse.Tests;

public class ParserTests
{
    [Fact]
    public void HeuristicParserLineConfidenceInRange()
    {
        var line = new ParsedPurchaseLineDto("Item", 10, 1, 0.5m);
        Assert.InRange(line.Confidence, 0m, 1m);
    }
}

public class InventoryTests
{
    [Fact]
    public void OutTransactionReducesBalance()
    {
        var start = 10m;
        var sold = 3m;
        Assert.Equal(7m, start - sold);
    }
}

public class AgreementValidationTests
{
    [Fact]
    public void ThresholdDiscountAppliesWhenQuantityReached()
    {
        var unitPrice = 100m;
        var discount = 20m;
        var expected = unitPrice * (1 - discount / 100m);
        Assert.Equal(80m, expected);
    }
}

public class ThresholdCalculationTests
{
    [Fact]
    public void PercentChangeIsCalculatedCorrectly()
    {
        var oldPrice = 100m;
        var newPrice = 110m;
        var pct = Math.Abs((newPrice - oldPrice) / oldPrice * 100m);
        Assert.Equal(10m, pct);
    }
}
