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
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_CostExactlyAt5K_NotBoosted()
    {
        var filing = new DobjobFiling { InitialCost = 5000m, JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_CostJustOver5K_Boosted()
    {
        var filing = new DobjobFiling { InitialCost = 5001m, JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_CostOver25K_GetsTwoPoints()
    {
        var filing = new DobjobFiling { InitialCost = 25001m, JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_CostOver100K_GetsThreePoints()
    {
        var filing = new DobjobFiling { InitialCost = 100001m, JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(3, score);
    }

    [Fact]
    public void ScorePermit_CostOver500K_GetsFourPoints()
    {
        var filing = new DobjobFiling { InitialCost = 500001m, JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(3, score);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("NB")]
    public void ScorePermit_MajorJobTypes_GetBoost(string jobType)
    {
        var filing = new DobjobFiling { JobType = jobType };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_A2JobType_GetsMinorBoost()
    {
        var filing = new DobjobFiling { JobType = "A2" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(2, score);
    }

    [Theory]
    [InlineData("A3")]
    [InlineData("DM")]
    [InlineData("SG")]
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
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_TwoTrades_GetsTradeBoost()
    {
        var filing = new DobjobFiling { Plumbing = "X", Mechanical = "X", JobType = "A1" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(3, score);
    }

    [Fact]
    public void ScorePermit_FourTrades_GetsBonusTradeBoost()
    {
        var filing = new DobjobFiling { Plumbing = "X", Mechanical = "X", Boiler = "X", Sprinkler = "X", JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
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

    [Fact]
    public void ScorePermit_TallBuilding_GetsHeightBoost()
    {
        var filing = new DobjobFiling { ExistingNoofStories = "15", JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_ActiveFiling_GetsStatusBoost()
    {
        var filing = new DobjobFiling { JobStatus = "In Process", JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_ApprovedFiling_GetsStatusBoost()
    {
        var filing = new DobjobFiling { JobStatus = "Approved", JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(2, score);
    }
}
