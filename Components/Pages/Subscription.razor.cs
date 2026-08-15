using Microsoft.AspNetCore.Components;
using Radzen;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;

namespace KonXProWebApp.Components.Pages
{
    public partial class Subscription
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected SecurityService Security { get; set; }

        [Inject]
        protected StripeService StripeService { get; set; }

        [Inject]
        protected SubscriptionTierService SubscriptionTierService { get; set; }

        protected string currentTier = "Free";
        protected Models.PermitIntel.Subscription currentSubscription;
        protected List<TierInfo> tiers = new();
        protected bool isPortalLoading = false;
        protected string subscribingTier = null;

        protected override async Task OnInitializedAsync()
        {
            tiers = StripeService.GetTierInfo();

            var userId = Security?.User?.Id;
            if (!string.IsNullOrEmpty(userId))
            {
                currentTier = await SubscriptionTierService.GetUserActiveTier(userId);
                currentSubscription = await SubscriptionTierService.GetUserSubscription(userId);
            }
        }

        protected async Task SubscribeToTier(string tierName)
        {
            subscribingTier = tierName;
            try
            {
                var userId = Security?.User?.Id ?? "guest";
                var email = Security?.User?.Email ?? "user@example.com";
                var successUrl = NavigationManager.BaseUri + "subscription";
                var cancelUrl = NavigationManager.BaseUri + "subscription";

                var checkoutUrl = await StripeService.CreateCheckoutSession(
                    userId, email, tierName, successUrl, cancelUrl);

                if (!string.IsNullOrEmpty(checkoutUrl))
                {
                    NavigationManager.NavigateTo(checkoutUrl, forceLoad: true);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Checkout Error", "Unable to initialize Stripe checkout session.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Checkout Failed", ex.Message);
            }
            finally
            {
                subscribingTier = null;
            }
        }

        protected async Task ManageSubscription()
        {
            isPortalLoading = true;
            try
            {
                var userId = Security?.User?.Id;
                var returnUrl = NavigationManager.BaseUri + "subscription";
                var url = await StripeService.CreatePortalSession(userId, returnUrl);

                if (!string.IsNullOrEmpty(url))
                {
                    NavigationManager.NavigateTo(url, forceLoad: true);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Portal Error", "Unable to launch billing portal.");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Billing Portal", ex.Message);
            }
            finally
            {
                isPortalLoading = false;
            }
        }

        protected Radzen.ButtonStyle GetButtonStyle(string styleName)
        {
            return styleName switch
            {
                "Primary" => Radzen.ButtonStyle.Primary,
                "Secondary" => Radzen.ButtonStyle.Secondary,
                "Info" => Radzen.ButtonStyle.Info,
                "Warning" => Radzen.ButtonStyle.Warning,
                "Danger" => Radzen.ButtonStyle.Danger,
                _ => Radzen.ButtonStyle.Light
            };
        }
    }
}