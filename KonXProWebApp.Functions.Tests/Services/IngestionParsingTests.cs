using KonXProWebApp.Functions.Models;
using KonXProWebApp.Functions.Services;
using Xunit;

namespace KonXProWebApp.Functions.Tests.Services;

public class IngestionParsingTests
{
    // ── ParseDate ──

    [Theory]
    [InlineData("01/15/2024", 2024, 1, 15)]
    [InlineData("12/31/2023", 2023, 12, 31)]
    public void ParseDate_SocrataFormat_ReturnsParsedDate(string input, int y, int m, int d)
    {
        var result = IngestionService.ParseDate(input);
        Assert.Equal(new DateTime(y, m, d), result);
    }

    [Fact]
    public void ParseDate_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(IngestionService.ParseDate(null));
        Assert.Null(IngestionService.ParseDate(""));
        Assert.Null(IngestionService.ParseDate("   "));
    }

    [Fact]
    public void ParseDate_Garbage_ReturnsNull()
    {
        Assert.Null(IngestionService.ParseDate("not-a-date"));
    }

    // ── ParseIsoDate ──

    [Theory]
    [InlineData("2022-02-24T00:00:00.000", 2022, 2, 24)]
    [InlineData("2023-11-15T00:00:00.000", 2023, 11, 15)]
    public void ParseIsoDate_SocrataFormat_ReturnsParsedDate(string input, int y, int m, int d)
    {
        var result = IngestionService.ParseIsoDate(input);
        Assert.Equal(new DateTime(y, m, d), result?.Date);
    }

    [Fact]
    public void ParseIsoDate_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(IngestionService.ParseIsoDate(null));
        Assert.Null(IngestionService.ParseIsoDate(""));
    }

    // ── ParseDobDate ──

    [Theory]
    [InlineData("20231231", 2023, 12, 31)]
    [InlineData("19990101", 1999, 1, 1)]
    public void ParseDobDate_YyyyMmDdFormat_ReturnsParsedDate(string input, int y, int m, int d)
    {
        var result = IngestionService.ParseDobDate(input);
        Assert.Equal(new DateTime(y, m, d), result);
    }

    [Fact]
    public void ParseDobDate_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(IngestionService.ParseDobDate(null));
        Assert.Null(IngestionService.ParseDobDate(""));
    }

    [Fact]
    public void ParseDobDate_FallsBackToGeneralParse()
    {
        // Not in yyyyMMdd shape, but a parseable roundtrip-ish date string
        var result = IngestionService.ParseDobDate("2023-12-31T00:00:00.000");
        Assert.Equal(new DateTime(2023, 12, 31), result?.Date);
    }

    // ── ParseCurrency ──

    [Theory]
    [InlineData("$50,000.00", 50000.00)]
    [InlineData("1,200", 1200.00)]
    [InlineData("$0", 0.00)]
    [InlineData("100", 100.00)]
    public void ParseCurrency_ValidFormats_ReturnsDecimal(string input, decimal expected)
    {
        var result = IngestionService.ParseCurrency(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("N/A")]
    [InlineData("not a number")]
    public void ParseCurrency_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(IngestionService.ParseCurrency(input));
    }

    // ── ParseInt ──

    [Fact]
    public void ParseInt_ValidNumber_ReturnsInt()
    {
        Assert.Equal(500001, IngestionService.ParseInt("500001"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    public void ParseInt_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(IngestionService.ParseInt(input));
    }

    // ── ComputeLeadScore ──

    [Fact]
    public void ComputeLeadScore_MinimalRecord_ReturnsOne()
    {
        var record = new SocrataPermitRecord { JobType = "A3" };
        Assert.Equal(1, IngestionService.ComputeLeadScore(record));
    }

    [Fact]
    public void ComputeLeadScore_HighCostNewBuildingWithTradesAndExpansion_ClampedAtFive()
    {
        var record = new SocrataPermitRecord
        {
            JobType = "NB",
            InitialCost = "$200,000",
            Plumbing = "X",
            Mechanical = "X",
            ExistingDwellingUnits = "1",
            ProposedDwellingUnits = "20"
        };
        Assert.Equal(5, IngestionService.ComputeLeadScore(record));
    }

    [Fact]
    public void ComputeLeadScore_CostOver10KAndMajorAlteration_ReturnsTwo()
    {
        var record = new SocrataPermitRecord { JobType = "A1", InitialCost = "$15,000" };
        // +1 cost > $10K, +1 job type A1 = 2
        Assert.Equal(2, IngestionService.ComputeLeadScore(record));
    }

    [Fact]
    public void ComputeLeadScore_CostBelow10K_ClampedAtOne()
    {
        var record = new SocrataPermitRecord { JobType = "A3", InitialCost = "$5,000" };
        Assert.Equal(1, IngestionService.ComputeLeadScore(record));
    }
}
