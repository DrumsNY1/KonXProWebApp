using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class Subscribe
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public db_9f8bee_konxdevService db_9f8bee_konxdevService { get; set; }

        [Inject]
        protected SecurityService Security { get; set; }

        [Inject]
        public KonXProWebApp.Services.PermitIntelService PermitIntelService { get; set; }

        [Inject]
        public KonXProWebApp.Services.StripeService StripeService { get; set; }

        protected List<TierInfo> tiers = KonXProWebApp.Services.StripeService.GetTierInfo();
        protected KonXProWebApp.Models.PermitIntel.Subscription currentSubscription;
        protected string processingTier;

        protected override async Task OnInitializedAsync()
        {
            var userId = Security.User?.Id;
            if (!string.IsNullOrEmpty(userId))
            {
                currentSubscription = await PermitIntelService.GetActiveSubscription(userId);
            }
        }

        protected async Task StartCheckout(string tier)
        {
            var userId = Security.User?.Id;
            var email = Security.User?.Email;

            if (string.IsNullOrEmpty(userId))
            {
                NavigationManager.NavigateTo("/Login");
                return;
            }

            processingTier = tier;
            try
            {
                var baseUrl = NavigationManager.BaseUri.TrimEnd('/');
                var checkoutUrl = await StripeService.CreateCheckoutSession(
                    userId,
                    email,
                    tier,
                    $"{baseUrl}/permit-intel/subscribe",
                    $"{baseUrl}/permit-intel/subscribe");

                // Redirect to Stripe Checkout
                await JSRuntime.InvokeVoidAsync("open", checkoutUrl, "_self");
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
            finally
            {
                processingTier = null;
            }
        }

        protected async Task OpenBillingPortal()
        {
            var userId = Security.User?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            try
            {
                var baseUrl = NavigationManager.BaseUri.TrimEnd('/');
                var portalUrl = await StripeService.CreatePortalSession(
                    userId, $"{baseUrl}/permit-intel/subscribe");

                await JSRuntime.InvokeVoidAsync("open", portalUrl, "_self");
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
        }

        protected string GetButtonText(string tier)
        {
            if (IsCurrentTier(tier)) return "Current Plan";
            return currentSubscription != null ? $"Switch to {tier}" : "Start Free Trial";
        }

        protected bool IsCurrentTier(string tier)
        {
            return currentSubscription?.Tier == tier &&
                   currentSubscription.Status is "Active" or "Trialing";
        }

        protected Radzen.ButtonStyle GetButtonStyle(string style)
        {
            return style switch
            {
                "Primary" => Radzen.ButtonStyle.Primary,
                "Secondary" => Radzen.ButtonStyle.Secondary,
                "Info" => Radzen.ButtonStyle.Info,
                _ => Radzen.ButtonStyle.Light
            };
        }
    }
}
