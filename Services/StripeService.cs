using KonXProWebApp.Data;
using KonXProWebApp.Models.PermitIntel;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace KonXProWebApp.Services;

/// <summary>
/// Manages Stripe checkout sessions, portal sessions, and subscription lifecycle.
/// </summary>
public class StripeService
{
    private readonly IConfiguration _configuration;
    private readonly db_9f8bee_konxdevContext _context;
    private readonly ILogger<StripeService> _logger;

    // Tier → Stripe Price ID mapping (configure in appsettings)
    private readonly Dictionary<string, string> _tierPriceIds;

    public StripeService(
        IConfiguration configuration,
        db_9f8bee_konxdevContext context,
        ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"] ?? "";

        _tierPriceIds = new Dictionary<string, string>
        {
            ["Starter"] = _configuration["Stripe:Prices:Starter"] ?? "",
            ["Pro"] = _configuration["Stripe:Prices:Pro"] ?? "",
            ["Business"] = _configuration["Stripe:Prices:Business"] ?? "",
            ["Agency"] = _configuration["Stripe:Prices:Agency"] ?? ""
        };
    }

    /// <summary>
    /// Creates a Stripe Checkout session with a 14-day free trial.
    /// </summary>
    public async Task<string> CreateCheckoutSession(
        string userId, string email, string tier, string successUrl, string cancelUrl)
    {
        var priceId = _tierPriceIds.GetValueOrDefault(tier)
            ?? throw new ArgumentException($"Unknown tier: {tier}");

        // Find or create Stripe customer
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        string customerId = subscription?.StripeCustomerId;

        if (string.IsNullOrEmpty(customerId))
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId
                }
            });
            customerId = customer.Id;
        }

        var options = new SessionCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = priceId,
                    Quantity = 1
                }
            },
            Mode = "subscription",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                TrialPeriodDays = 14,
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["tier"] = tier
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId,
                ["tier"] = tier
            }
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(options);

        _logger.LogInformation("Checkout session created for user {UserId}, tier {Tier}: {SessionId}",
            userId, tier, session.Id);

        return session.Url;
    }

    /// <summary>
    /// Creates a Stripe Billing Portal session for managing subscriptions.
    /// </summary>
    public async Task<string> CreatePortalSession(string userId, string returnUrl)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.StripeCustomerId != null);

        if (subscription == null)
            throw new InvalidOperationException("No subscription found for this user.");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = subscription.StripeCustomerId,
            ReturnUrl = returnUrl
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    /// <summary>
    /// Handles a completed checkout session — creates the local subscription record.
    /// </summary>
    public async Task HandleCheckoutCompleted(Session session)
    {
        var userId = session.Metadata.GetValueOrDefault("userId");
        var tier = session.Metadata.GetValueOrDefault("tier") ?? "Starter";

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Checkout completed but no userId in metadata: {SessionId}", session.Id);
            return;
        }

        // Deactivate any existing subscriptions
        var existing = await _context.Subscriptions
            .Where(s => s.UserId == userId && (s.Status == "Active" || s.Status == "Trialing"))
            .ToListAsync();

        foreach (var sub in existing)
        {
            sub.Status = "Superseded";
            sub.EndDate = DateTime.UtcNow;
            sub.UpdatedAt = DateTime.UtcNow;
        }

        // Create new subscription record
        var newSubscription = new Models.PermitIntel.Subscription
        {
            UserId = userId,
            StripeCustomerId = session.CustomerId,
            StripeSubscriptionId = session.SubscriptionId,
            Tier = tier,
            Status = "Trialing",
            StartDate = DateTime.UtcNow,
            TrialEndDate = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(newSubscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription created for user {UserId}: {Tier} (trial until {TrialEnd})",
            userId, tier, newSubscription.TrialEndDate);
    }

    /// <summary>
    /// Handles subscription status updates from Stripe webhooks.
    /// </summary>
    public async Task HandleSubscriptionUpdated(Stripe.Subscription stripeSubscription)
    {
        var localSub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (localSub == null)
        {
            _logger.LogWarning("Subscription update for unknown sub: {SubId}", stripeSubscription.Id);
            return;
        }

        localSub.Status = stripeSubscription.Status switch
        {
            "active" => "Active",
            "trialing" => "Trialing",
            "past_due" => "PastDue",
            "canceled" => "Canceled",
            "unpaid" => "PastDue",
            _ => stripeSubscription.Status
        };

        if (stripeSubscription.Status == "canceled")
            localSub.EndDate = DateTime.UtcNow;

        localSub.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription {SubId} updated to status {Status}",
            stripeSubscription.Id, localSub.Status);
    }

    /// <summary>
    /// Gets the tier display info for the pricing page.
    /// </summary>
    public static List<TierInfo> GetTierInfo()
    {
        return new List<TierInfo>
        {
            new()
            {
                Name = "Starter",
                Price = 29,
                Description = "Perfect for solo contractors",
                Features = new List<string>
                {
                    "50 permit searches / day",
                    "Save up to 25 leads",
                    "Daily email alerts",
                    "Basic lead scoring",
                    "14-day free trial"
                },
                ButtonStyle = "Light",
                IsPopular = false
            },
            new()
            {
                Name = "Pro",
                Price = 79,
                Description = "For growing businesses",
                Features = new List<string>
                {
                    "Unlimited searches",
                    "Save up to 100 leads",
                    "Daily + instant alerts",
                    "Email + SMS notifications",
                    "Advanced lead scoring",
                    "Export to CSV/Excel",
                    "14-day free trial"
                },
                ButtonStyle = "Primary",
                IsPopular = true
            },
            new()
            {
                Name = "Business",
                Price = 149,
                Description = "For established firms",
                Features = new List<string>
                {
                    "Everything in Pro",
                    "Unlimited leads",
                    "Map visualization",
                    "Team sharing (up to 5)",
                    "Priority support",
                    "14-day free trial"
                },
                ButtonStyle = "Secondary",
                IsPopular = false
            },
            new()
            {
                Name = "Agency",
                Price = 299,
                Description = "For agencies & enterprises",
                Features = new List<string>
                {
                    "Everything in Business",
                    "Unlimited team members",
                    "API access",
                    "Custom integrations",
                    "Dedicated support",
                    "14-day free trial"
                },
                ButtonStyle = "Info",
                IsPopular = false
            }
        };
    }
}

public class TierInfo
{
    public string Name { get; set; }
    public int Price { get; set; }
    public string Description { get; set; }
    public List<string> Features { get; set; } = new();
    public string ButtonStyle { get; set; }
    public bool IsPopular { get; set; }
}
