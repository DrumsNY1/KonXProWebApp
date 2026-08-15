using KonXProWebApp.Functions.Functions;
using Xunit;

namespace KonXProWebApp.Functions.Tests.Functions;

public class AlertDispatchMatchingTests
{
    [Fact]
    public void BuildWhereClause_BoroughFilter_ProducesParameterizedIn()
    {
        var user = new AlertDispatchFunction.AlertUser { Boroughs = "BROOKLYN,QUEENS" };
        var (sql, parameters) = AlertDispatchFunction.BuildWhereClause(user);

        Assert.Contains("Borough IN", sql);
        Assert.Equal(2, parameters.Count(p => p.ParameterName.StartsWith("@borough")));
        Assert.DoesNotContain("BROOKLYN", sql); // must be parameterized, not inlined
        Assert.Contains(parameters, p => p.ParameterName == "@borough0" && (string)p.Value! == "BROOKLYN");
        Assert.Contains(parameters, p => p.ParameterName == "@borough1" && (string)p.Value! == "QUEENS");
    }

    [Fact]
    public void BuildWhereClause_NoFilters_OnlyDateCondition()
    {
        var user = new AlertDispatchFunction.AlertUser();
        var (sql, parameters) = AlertDispatchFunction.BuildWhereClause(user);

        Assert.Contains("LatestActionDate >= DATEADD", sql);
        Assert.DoesNotContain("Borough IN", sql);
        Assert.DoesNotContain("JobType IN", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void BuildWhereClause_JobTypeFilter_ProducesParameterizedIn()
    {
        var user = new AlertDispatchFunction.AlertUser { JobTypes = "A1,NB" };
        var (sql, parameters) = AlertDispatchFunction.BuildWhereClause(user);

        Assert.Contains("JobType IN", sql);
        Assert.Equal(2, parameters.Count(p => p.ParameterName.StartsWith("@jobType")));
        Assert.DoesNotContain("'A1'", sql);
    }

    [Fact]
    public void BuildWhereClause_CostRange_AddsBothConditions()
    {
        var user = new AlertDispatchFunction.AlertUser { MinCost = 10_000m, MaxCost = 100_000m };
        var (sql, parameters) = AlertDispatchFunction.BuildWhereClause(user);

        Assert.Contains("InitialCost >= @minCost", sql);
        Assert.Contains("InitialCost <= @maxCost", sql);
        Assert.Contains(parameters, p => p.ParameterName == "@minCost" && (decimal)p.Value! == 10_000m);
        Assert.Contains(parameters, p => p.ParameterName == "@maxCost" && (decimal)p.Value! == 100_000m);
    }

    [Fact]
    public void BuildWhereClause_TradeFilters_UsesOrNotAnd()
    {
        var user = new AlertDispatchFunction.AlertUser { Trades = "Plumbing,Mechanical" };
        var (sql, _) = AlertDispatchFunction.BuildWhereClause(user);

        // Trade conditions within a single filing should be OR'd (any matching trade)
        Assert.Contains("Plumbing = 'X'", sql);
        Assert.Contains("Mechanical = 'X'", sql);
        Assert.Contains("OR", sql);
    }

    [Fact]
    public void BuildWhereClause_UnknownTrade_IsIgnored()
    {
        var user = new AlertDispatchFunction.AlertUser { Trades = "NotARealTrade" };
        var (sql, _) = AlertDispatchFunction.BuildWhereClause(user);

        // No known trade conditions matched, so no trade group should be appended.
        Assert.DoesNotContain("'X'", sql);
    }

    [Fact]
    public void BuildWhereClause_AllFilters_CombinedWithAnd()
    {
        var user = new AlertDispatchFunction.AlertUser
        {
            Boroughs = "BROOKLYN",
            JobTypes = "NB",
            MinCost = 5_000m,
            Trades = "Boiler"
        };
        var (sql, parameters) = AlertDispatchFunction.BuildWhereClause(user);

        Assert.Contains("Borough IN", sql);
        Assert.Contains("JobType IN", sql);
        Assert.Contains("InitialCost >= @minCost", sql);
        Assert.Contains("Boiler = 'X'", sql);
        Assert.Contains(" AND ", sql);
        Assert.Equal(3, parameters.Count); // borough0, jobType0, minCost
    }

    [Fact]
    public void BuildWhereClause_SelectsTop50OrderedByLeadScore()
    {
        var user = new AlertDispatchFunction.AlertUser();
        var (sql, _) = AlertDispatchFunction.BuildWhereClause(user);

        Assert.Contains("TOP 50", sql);
        Assert.Contains("ORDER BY d.LeadScore DESC", sql);
    }

    [Fact]
    public void BuildWhereClause_MinLeadScore_AddsCondition()
    {
        var user = new AlertDispatchFunction.AlertUser { MinLeadScore = 4 };
        var (sql, parameters) = AlertDispatchFunction.BuildWhereClause(user);

        Assert.Contains("LeadScore >= @minLeadScore", sql);
        Assert.Contains(parameters, p => p.ParameterName == "@minLeadScore" && (int)p.Value! == 4);
    }

    [Fact]
    public void BuildAlertDigestHtml_RendersScoreBadgesAndAddress()
    {
        var emailService = new KonXProWebApp.Functions.Services.EmailService(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<KonXProWebApp.Functions.Services.EmailService>());

        var matches = new List<KonXProWebApp.Functions.Services.AlertPermitMatch>
        {
            new() { PermitId = 1, Address = "100 Broadway", Borough = "MANHATTAN", JobType = "NB", EstCost = "$500,000", Status = "Approved", LeadScore = 5, ScoreTier = "Hot", FactorSummary = "High Cost • Class C HPD Violation" }
        };

        var html = emailService.BuildAlertDigestHtml("Reggie", matches);

        Assert.Contains("100 Broadway", html);
        Assert.Contains("HOT", html);
        Assert.Contains("High Cost • Class C HPD Violation", html);
    }

    [Fact]
    public void BuildAlertSms_FormatsScoreStarAndTier()
    {
        var smsText = KonXProWebApp.Functions.Services.SmsService.BuildAlertSms(3, "500 Grand St", 4, "Hot");

        Assert.Contains("🔥 NYC Permit Intel: 3 new permit lead(s)", smsText);
        Assert.Contains("500 Grand St (4★ HOT)", smsText);
    }
}
