using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Services;
using Xunit;

namespace KonXProWebApp.Tests.Services;

public class BblComputationTests
{
    [Theory]
    [InlineData("MANHATTAN", "00123", "0045", "1001230045")]
    [InlineData("BROOKLYN", "00456", "0012", "3004560012")]
    [InlineData("QUEENS", "01234", "0056", "4012340056")]
    [InlineData("BRONX", "00001", "0001", "2000010001")]
    [InlineData("STATEN ISLAND", "00099", "0003", "5000990003")]
    public void GetBblFromFiling_KnownBorough_ReturnsCorrectBbl(
        string borough, string block, string lot, string expectedBbl)
    {
        var filing = new DobjobFiling { Borough = borough, Block = block, Lot = lot };
        Assert.Equal(expectedBbl, PermitIntelService.GetBblFromFiling(filing));
    }

    [Fact]
    public void GetBblFromFiling_CaseInsensitiveBorough_StillResolves()
    {
        var filing = new DobjobFiling { Borough = "manhattan", Block = "00123", Lot = "0045" };
        Assert.Equal("1001230045", PermitIntelService.GetBblFromFiling(filing));
    }

    [Fact]
    public void GetBblFromFiling_UnknownBorough_ReturnsNull()
    {
        var filing = new DobjobFiling { Borough = "NEW JERSEY", Block = "00001", Lot = "0001" };
        Assert.Null(PermitIntelService.GetBblFromFiling(filing));
    }

    [Theory]
    [InlineData(null, "00001", "0001")]
    [InlineData("MANHATTAN", null, "0001")]
    [InlineData("MANHATTAN", "00001", null)]
    [InlineData("", "00001", "0001")]
    public void GetBblFromFiling_MissingField_ReturnsNull(string? borough, string? block, string? lot)
    {
        var filing = new DobjobFiling { Borough = borough, Block = block, Lot = lot };
        Assert.Null(PermitIntelService.GetBblFromFiling(filing));
    }

    [Fact]
    public void GetBblFromFiling_ShortBlockAndLot_PadsToCorrectWidth()
    {
        // Block "1" → "00001", Lot "1" → "0001"
        var filing = new DobjobFiling { Borough = "MANHATTAN", Block = "1", Lot = "1" };
        Assert.Equal("1000010001", PermitIntelService.GetBblFromFiling(filing));
    }
}
