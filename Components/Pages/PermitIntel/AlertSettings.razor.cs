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

        // Options
        protected List<BoroughOption> boroughOptions;
        protected List<JobTypeOption> jobTypeOptions;
        protected List<TradeOption> tradeOptions;

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

            // Parse comma-separated values
            var savedBoroughs = string.IsNullOrEmpty(preference.Boroughs)
                ? new List<string>()
                : preference.Boroughs.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var savedJobTypes = string.IsNullOrEmpty(preference.JobTypes)
                ? new List<string>()
                : preference.JobTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var savedTrades = string.IsNullOrEmpty(preference.Trades)
                ? new List<string>()
                : preference.Trades.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Initialize options with selection states
            boroughOptions = new()
            {
                new() { Name = "MANHATTAN", Selected = savedBoroughs.Contains("MANHATTAN") },
                new() { Name = "BROOKLYN", Selected = savedBoroughs.Contains("BROOKLYN") },
                new() { Name = "QUEENS", Selected = savedBoroughs.Contains("QUEENS") },
                new() { Name = "BRONX", Selected = savedBoroughs.Contains("BRONX") },
                new() { Name = "STATEN ISLAND", Selected = savedBoroughs.Contains("STATEN ISLAND") }
            };

            jobTypeOptions = new()
            {
                new() { Key = "A1", Name = "A1 (Major)", Selected = savedJobTypes.Contains("A1") },
                new() { Key = "A2", Name = "A2 (Minor)", Selected = savedJobTypes.Contains("A2") },
                new() { Key = "A3", Name = "A3 (Minor)", Selected = savedJobTypes.Contains("A3") },
                new() { Key = "NB", Name = "New Building", Selected = savedJobTypes.Contains("NB") },
                new() { Key = "DM", Name = "Demolition", Selected = savedJobTypes.Contains("DM") }
            };

            tradeOptions = new()
            {
                new() { Key = "Plumbing", Name = "Plumbing", Selected = savedTrades.Contains("Plumbing") },
                new() { Key = "Mechanical", Name = "Mechanical", Selected = savedTrades.Contains("Mechanical") },
                new() { Key = "Boiler", Name = "Boiler", Selected = savedTrades.Contains("Boiler") },
                new() { Key = "Sprinkler", Name = "Sprinkler", Selected = savedTrades.Contains("Sprinkler") },
                new() { Key = "FireAlarm", Name = "Fire Alarm", Selected = savedTrades.Contains("FireAlarm") },
                new() { Key = "Standpipe", Name = "Standpipe", Selected = savedTrades.Contains("Standpipe") },
                new() { Key = "Equipment", Name = "Equipment", Selected = savedTrades.Contains("Equipment") },
                new() { Key = "FireSuppression", Name = "Fire Suppression", Selected = savedTrades.Contains("FireSuppression") },
                new() { Key = "CurbCut", Name = "Curb Cut", Selected = savedTrades.Contains("CurbCut") }
            };
        }

        protected async Task SaveSettings()
        {
            isSaving = true;
            try
            {
                // Serialize lists to comma-separated strings
                var activeBoroughs = boroughOptions.Where(b => b.Selected).Select(b => b.Name).ToList();
                preference.Boroughs = activeBoroughs.Any() ? string.Join(",", activeBoroughs) : null;

                var activeJobTypes = jobTypeOptions.Where(j => j.Selected).Select(j => j.Key).ToList();
                preference.JobTypes = activeJobTypes.Any() ? string.Join(",", activeJobTypes) : null;

                var activeTrades = tradeOptions.Where(t => t.Selected).Select(t => t.Key).ToList();
                preference.Trades = activeTrades.Any() ? string.Join(",", activeTrades) : null;

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

        // Helper classes
        public class BoroughOption
        {
            public string Name { get; set; }
            public bool Selected { get; set; }
        }

        public class JobTypeOption
        {
            public string Name { get; set; }
            public string Key { get; set; }
            public bool Selected { get; set; }
        }

        public class TradeOption
        {
            public string Name { get; set; }
            public string Key { get; set; }
            public bool Selected { get; set; }
        }

        public class DropDownItem
        {
            public string Text { get; set; }
            public string Value { get; set; }
        }
    }
}
