using KonXProWebApp.Data;
using KonXProWebApp.Models.PermitIntel;
using Microsoft.EntityFrameworkCore;

namespace KonXProWebApp.Services;

public class SubscriptionTierService
{
    private readonly db_9f8bee_konxdevContext _context;

    public SubscriptionTierService(db_9f8bee_konxdevContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the user's active subscription tier (defaults to "Free").
    /// </summary>
    public async Task<string> GetUserActiveTier(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return "Free";

        var sub = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && (s.Status == "Active" || s.Status == "Trialing"))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        return sub?.Tier ?? "Free";
    }

    /// <summary>
    /// Retrieves full subscription details for a user.
    /// </summary>
    public async Task<Subscription> GetUserSubscription(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        return await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && (s.Status == "Active" || s.Status == "Trialing"))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Checks whether a tier satisfies a required feature entitlement.
    /// </summary>
    public static bool TierSatisfiesRequirement(string currentTier, string requiredTier)
    {
        var hierarchy = new List<string> { "Free", "Starter", "ComplianceAlerts", "Pro", "LandlordCompliance", "Business", "Agency" };

        int currentIndex = hierarchy.IndexOf(currentTier ?? "Free");
        int requiredIndex = hierarchy.IndexOf(requiredTier ?? "Free");

        if (currentIndex == -1) currentIndex = 0;
        if (requiredIndex == -1) requiredIndex = 0;

        return currentIndex >= requiredIndex;
    }

    /// <summary>
    /// Checks feature access based on current user's tier.
    /// </summary>
    public async Task<bool> HasFeatureAccess(string userId, string featureKey)
    {
        var tier = await GetUserActiveTier(userId);

        return featureKey switch
        {
            "SearchPermits" => true, // Available to all tiers
            "ExportData" => TierSatisfiesRequirement(tier, "Pro"),
            "PermitMap" => TierSatisfiesRequirement(tier, "Business"),
            "InstantAlerts" => TierSatisfiesRequirement(tier, "Pro"),
            "ContractorAnalytics" => TierSatisfiesRequirement(tier, "Starter"),
            _ => true
        };
    }
}
