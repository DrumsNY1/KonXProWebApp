using KonXProWebApp.Functions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KonXProWebApp.Functions.Tests.Services;

public class EmailServiceTests
{
    private static EmailService CreateService() =>
        new(new ConfigurationBuilder().Build(), new Mock<ILogger<EmailService>>().Object);

    [Fact]
    public void BuildAlertDigestHtml_SingleMatch_ContainsAddressAndDetailLink()
    {
        var service = CreateService();

        var matches = new List<AlertPermitMatch>
        {
            new()
            {
                PermitId = 42, Address = "123 Main St", Borough = "BROOKLYN",
                JobType = "NB", EstCost = "$150,000", Status = "Approved", LeadScore = 4
            }
        };

        var html = service.BuildAlertDigestHtml("Alice", matches);

        Assert.Contains("123 Main St", html);
        Assert.Contains("BROOKLYN", html);
        Assert.Contains("/permit-intel/detail/42", html);
        Assert.Contains("1", html);
        Assert.Contains("Alice", html);
    }

    [Fact]
    public void BuildAlertDigestHtml_MultipleMatches_ContainsOneRowPerMatch()
    {
        var service = CreateService();

        var matches = new List<AlertPermitMatch>
        {
            new() { PermitId = 1, Address = "1 First Ave", Borough = "QUEENS", JobType = "A1", EstCost = "$10,000", Status = "New", LeadScore = 2 },
            new() { PermitId = 2, Address = "2 Second Ave", Borough = "BRONX", JobType = "NB", EstCost = "$20,000", Status = "New", LeadScore = 3 }
        };

        var html = service.BuildAlertDigestHtml("Bob", matches);

        Assert.Contains("/permit-intel/detail/1", html);
        Assert.Contains("/permit-intel/detail/2", html);
    }

    [Fact]
    public void BuildAlertDigestHtml_EmptyMatches_DoesNotThrow()
    {
        var service = CreateService();

        var html = service.BuildAlertDigestHtml("Bob", new List<AlertPermitMatch>());

        Assert.NotNull(html);
        Assert.Contains("Bob", html);
    }

    [Fact]
    public void BuildAlertDigestHtml_LeadScore_RendersStarPerPoint()
    {
        var service = CreateService();

        var matches = new List<AlertPermitMatch>
        {
            new() { PermitId = 7, Address = "7 Star St", Borough = "MANHATTAN", JobType = "A1", EstCost = "$1", Status = "New", LeadScore = 3 }
        };

        var html = service.BuildAlertDigestHtml("Carol", matches);

        // ⭐ (U+2B50) repeated once per lead-score point
        var starCount = html.Split('⭐').Length - 1;
        Assert.Equal(3, starCount);
    }
}
