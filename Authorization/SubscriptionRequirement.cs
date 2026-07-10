using KonXProWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KonXProWebApp.Authorization;

/// <summary>
/// Authorization requirement that checks if a user has an active subscription
/// at or above the specified tier level.
/// </summary>
public class SubscriptionRequirement : IAuthorizationRequirement
{
    public string MinimumTier { get; }

    public SubscriptionRequirement(string minimumTier)
    {
        MinimumTier = minimumTier;
    }

    /// <summary>
    /// Returns the tier level (higher = more features).
    /// </summary>
    public static int GetTierLevel(string tier)
    {
        return tier switch
        {
            "Starter" => 1,
            "Pro" => 2,
            "Business" => 3,
            "Agency" => 4,
            _ => 0
        };
    }
}

public class SubscriptionAuthorizationHandler : AuthorizationHandler<SubscriptionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SubscriptionAuthorizationHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SubscriptionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return;

        using var scope = _scopeFactory.CreateScope();
        var permitIntelService = scope.ServiceProvider.GetRequiredService<PermitIntelService>();

        var subscription = await permitIntelService.GetActiveSubscription(userId);
        if (subscription == null)
            return;

        var userLevel = SubscriptionRequirement.GetTierLevel(subscription.Tier);
        var requiredLevel = SubscriptionRequirement.GetTierLevel(requirement.MinimumTier);

        if (userLevel >= requiredLevel)
        {
            context.Succeed(requirement);
        }
    }
}
