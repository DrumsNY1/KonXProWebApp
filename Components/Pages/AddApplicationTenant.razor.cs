using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace KonXProWebApp.Components.Pages
{
    public partial class AddApplicationTenant
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

        protected KonXProWebApp.Models.ApplicationTenant tenant;
        protected string error;
        protected bool errorVisible;

        [Inject]
        protected SecurityService Security { get; set; }

        protected override async Task OnInitializedAsync()
        {
            tenant = new KonXProWebApp.Models.ApplicationTenant();
        }

        protected async Task FormSubmit(KonXProWebApp.Models.ApplicationTenant tenant)
        {
            try
            {
                await Security.CreateTenant(tenant);

                DialogService.Close(null);
            }
            catch (Exception ex)
            {
                errorVisible = true;
                error = ex.Message;
            }
        }

        protected async Task CancelClick()
        {
            DialogService.Close(null);
        }
    }
}