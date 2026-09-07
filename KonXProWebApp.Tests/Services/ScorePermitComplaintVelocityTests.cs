using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Services;
using Xunit;

namespace KonXProWebApp.Tests.Services;

public class ScorePermitComplaintVelocityTests
{
    [Fact]
    public void ScorePermit_ZeroComplaints_NoPredictiveBoost()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 0);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_OneComplaint_AddsOnePoint()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 1);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_TwoComplaints_StillAddsOnePoint()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 2);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_ThreeOrMoreComplaints_AddsTwoPoints()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 3);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_FullHouse_WithHighComplaints_ClampedAtFive()
    {
        var filing = new DobjobFiling
        {
            JobType = "NB",
            InitialCost = 200_000m,
            Plumbing = "X",
            Mechanical = "X",
            ExistingDwellingUnits = "1",
            ProposedDwellingUnits = "20"
        };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 10);
        Assert.Equal(5, score);
    }

    [Fact]
    public void ScorePermit_ComplaintBoostCombinedWithOtherFactors_AddsCorrectly()
    {
        var filing = new DobjobFiling { JobType = "A1", InitialCost = 15_000m };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 1);
        Assert.Equal(3, score);
    }

    [Fact]
    public void ScorePermit_ActiveDobViolations_AddsViolationBoost()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score1 = PermitIntelService.ScorePermit(filing, activeDobViolations: 1);
        Assert.Equal(2, score1);

        var score2 = PermitIntelService.ScorePermit(filing, activeDobViolations: 3);
        Assert.Equal(2, score2);
    }

    [Fact]
    public void ScorePermit_SevereHpdClassCViolation_AddsExtraTwoPoints()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing, hpdClassCCount: 1);
        Assert.Equal(2, score);
    }

    [Fact]
    public void ScorePermit_CombinedDobAndHpdClassC_BoostsScore()
    {
        var filing = new DobjobFiling { JobType = "A1", InitialCost = 60_000m };
        var score = PermitIntelService.ScorePermit(filing, activeDobViolations: 3, hpdClassCCount: 1);
        Assert.Equal(5, score);
    }
}
