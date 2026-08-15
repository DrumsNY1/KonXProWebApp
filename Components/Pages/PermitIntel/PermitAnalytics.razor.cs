using Microsoft.AspNetCore.Components;
using Radzen;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class PermitAnalytics
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public KonXProWebApp.Services.PermitIntelService PermitIntelService { get; set; }

        protected PermitAnalyticsSummary summary;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                summary = await PermitIntelService.GetAnalyticsSummary();
            }
            catch (Exception ex)
            {
                NotificationService?.Notify(NotificationSeverity.Error, "Analytics Error", ex.Message);
            }
        }
    }
}
