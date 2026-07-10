using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class AlertSettings
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

        protected AlertPreference preference;
        protected bool isSaving = false;

        // Filter state
        protected IEnumerable<string> selectedBoroughs;
        protected IEnumerable<string> selectedJobTypes;
        protected IEnumerable<string> selectedTrades;

        // Options
        protected List<string> boroughOptions = new() { "MANHATTAN", "BROOKLYN", "QUEENS", "BRONX", "STATEN ISLAND" };
        protected List<DropDownItem> jobTypeOptions = new()
        {
            new() { Text = "A1 - Major Alteration", Value = "A1" },
            new() { Text = "A2 - Minor Alteration", Value = "A2" },
            new() { Text = "A3 - Minor Alteration", Value = "A3" },
            new() { Text = "NB - New Building", Value = "NB" },
            new() { Text = "DM - Demolition", Value = "DM" },
        };
        protected List<string> tradeOptions = new()
        {
            "Plumbing", "Mechanical", "Boiler", "Sprinkler",
            "Fire Alarm", "Standpipe", "Equipment", "Fire Suppression", "Curb Cut"
        };

        protected override async Task OnInitializedAsync()
        {
            var userId = Security.User?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            preference = await PermitIntelService.GetAlertPreference(userId);
            if (preference == null)
            {
                preference = new AlertPreference
                {
                    UserId = userId,
                    AlertChannel = "Email",
                    AlertFrequency = "Daily",
                    IsActive = true
                };
            }

            // Parse comma-separated values back to lists
            selectedBoroughs = string.IsNullOrEmpty(preference.Boroughs)
                ? Enumerable.Empty<string>()
                : preference.Boroughs.Split(',', StringSplitOptions.RemoveEmptyEntries);

            selectedJobTypes = string.IsNullOrEmpty(preference.JobTypes)
                ? Enumerable.Empty<string>()
                : preference.JobTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);

            selectedTrades = string.IsNullOrEmpty(preference.Trades)
                ? Enumerable.Empty<string>()
                : preference.Trades.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        protected async Task SaveSettings()
        {
            isSaving = true;
            try
            {
                // Serialize lists to comma-separated strings
                preference.Boroughs = selectedBoroughs?.Any() == true
                    ? string.Join(",", selectedBoroughs) : null;
                preference.JobTypes = selectedJobTypes?.Any() == true
                    ? string.Join(",", selectedJobTypes) : null;
                preference.Trades = selectedTrades?.Any() == true
                    ? string.Join(",", selectedTrades) : null;

                await PermitIntelService.SaveAlertPreference(preference);

                NotificationService.Notify(NotificationSeverity.Success, "Saved",
                    "Alert settings updated successfully.");
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
            finally
            {
                isSaving = false;
            }
        }

        // Helper class
        public class DropDownItem
        {
            public string Text { get; set; }
            public string Value { get; set; }
        }
    }
}
