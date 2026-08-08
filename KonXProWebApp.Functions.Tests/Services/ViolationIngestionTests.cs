using KonXProWebApp.Functions.Services;
using Xunit;

namespace KonXProWebApp.Functions.Tests.Services;

public class ViolationIngestionTests
{
    [Theory]
    [InlineData("20240315", 2024, 3, 15)]
    [InlineData("20251201", 2025, 12, 1)]
    public void ParseDobDate_YyyyMmDdFormat_ParsesCorrectly(string input, int y, int m, int d)
    {
        var date = IngestionService.ParseDobDate(input);
        Assert.NotNull(date);
        Assert.Equal(new DateTime(y, m, d), date.Value);
    }

    [Fact]
    public void ParseDobDate_InvalidFormat_ReturnsNull()
    {
        var date = IngestionService.ParseDobDate("invalid-date");
        Assert.Null(date);
    }
}
