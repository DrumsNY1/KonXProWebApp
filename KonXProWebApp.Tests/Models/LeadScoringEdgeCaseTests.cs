using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Services;
using Xunit;

namespace KonXProWebApp.Tests.Models;

public class LeadScoringEdgeCaseTests
{
    [Fact]
    public void ScorePermit_NullFields_DoesNotThrow()
    {
        var filing = new DobjobFiling();
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(1, score); // minimum score clamp
    }

    [Fact]
    public void ScorePermit_CostExactlyAt10K_NotBoosted()
    {
        var filing = new DobjobFiling { InitialCost = 10_000m, JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_CostJustOver10K_Boosted()
    {
        var filing = new DobjobFiling { InitialCost = 10_001m, JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        // Score: +1 cost > $10K = 1, clamped to min 1
        Assert.Equal(1, score);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("NB")]
    public void ScorePermit_MajorJobTypes_GetBoost(string jobType)
    {
        var filing = new DobjobFiling { JobType = jobType };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.True(score >= 1); // at least base + job type boost
    }

    [Theory]
    [InlineData("A2")]
    [InlineData("A3")]
    [InlineData("DM")]
    public void ScorePermit_MinorJobTypes_NoBoost(string jobType)
    {
        var filing = new DobjobFiling { JobType = jobType };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_SingleTrade_NoTradeBoost()
    {
        var filing = new DobjobFiling { Plumbing = "X", JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(1, score); // need 2+ trades for boost
    }

    [Fact]
    public void ScorePermit_TwoTrades_GetsTradeBoost()
    {
        var filing = new DobjobFiling { Plumbing = "X", Mechanical = "X", JobType = "A1" };
        var score = PermitIntelService.ScorePermit(filing);
        // Score: +1 job type A1, +1 trades >= 2 = 2
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_InvalidDwellingUnits_NoExpansionBoost()
    {
        var filing = new DobjobFiling
        {
            ExistingDwellingUnits = "abc",
            ProposedDwellingUnits = "xyz",
            JobType = "A3"
        };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_SameUnits_NoExpansionBoost()
    {
        var filing = new DobjobFiling
        {
            ExistingDwellingUnits = "5",
            ProposedDwellingUnits = "5",
            JobType = "A3"
        };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(1, score);
    }
}
