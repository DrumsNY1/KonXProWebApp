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
        Assert.Equal(1, score); // base clamp only, no boost
    }

    [Fact]
    public void ScorePermit_OneComplaint_AddsOnePoint()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 1);
        // base score 0 + 1 (0 < velocity < 3) = 1
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_TwoComplaints_StillAddsOnePoint()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 2);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_ThreeOrMoreComplaints_AddsTwoPoints()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 3);
        // base score 0 + 2 (velocity >= 3) = 2, clamped to min 1 (no-op here since 2 >= 1)
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
        // +1 cost > 10K, +1 job type A1, +1 complaint boost (velocity 1) = 3
        var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 1);
        Assert.Equal(3, score);
    }
}
