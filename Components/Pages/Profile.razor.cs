using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace KonXProWebApp.Components.Pages
{
    public partial class Profile
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
        public KonXProWebApp.Services.PermitIntelService PermitIntelService { get; set; }

        [Inject]
        public KonXProWebApp.Services.StripeService StripeService { get; set; }

        protected string oldPassword = "";
        protected string newPassword = "";
        protected string confirmPassword = "";
        protected KonXProWebApp.Models.ApplicationUser user;
        protected KonXProWebApp.Models.PermitIntel.Subscription activeSubscription;
        protected string error;
        protected bool errorVisible;
        protected bool successVisible;

        // Profile details save state
        protected bool isSavingProfile = false;
        protected string profileError;
        protected bool profileErrorVisible;
        protected bool profileSuccessVisible;

        [Inject]
        protected SecurityService Security { get; set; }

        protected override async Task OnInitializedAsync()
        {
            user = await Security.GetUserById($"{Security.User.Id}");
            if (user != null)
            {
                activeSubscription = await PermitIntelService.GetActiveSubscription(user.Id);
            }
        }

        protected async Task OpenBillingPortal()
        {
            if (user == null) return;
            try
            {
                var returnUrl = NavigationManager.BaseUri.TrimEnd('/') + "/profile";
                var portalUrl = await StripeService.CreatePortalSession(user.Id, returnUrl);
                await JSRuntime.InvokeVoidAsync("open", portalUrl, "_self");
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
        }

        protected async Task SaveProfile()
        {
            isSavingProfile = true;
            profileErrorVisible = false;
            profileSuccessVisible = false;
            try
            {
                if (user != null)
                {
                    await Security.UpdateUser(user.Id, user);
                    profileSuccessVisible = true;
                    NotificationService.Notify(NotificationSeverity.Success, "Saved", "Profile details updated successfully.");
                }
            }
            catch (Exception ex)
            {
                profileErrorVisible = true;
                profileError = ex.Message;
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
            finally
            {
                isSavingProfile = false;
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                await Security.ChangePassword(oldPassword, newPassword);
                successVisible = true;
            }
            catch (Exception ex)
            {
                errorVisible = true;
                error = ex.Message;
            }
        }
    }
}