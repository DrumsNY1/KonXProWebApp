using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class PermitSearch
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

        // Data
        protected IEnumerable<DobjobFiling> permits;
        protected RadzenDataGrid<DobjobFiling> grid0;
        protected RadzenDropDown<IEnumerable<string>> boroughDropDown;
        protected RadzenDropDown<IEnumerable<string>> jobTypeDropDown;
        protected int totalCount = 0;

        // Filter state
        protected IEnumerable<string> selectedBoroughs;
        protected IEnumerable<string> selectedJobTypes;
        protected string searchText = "";
        protected decimal? minCost;
        protected decimal? maxCost;
        protected DateTime? dateFrom;
        protected DateTime? dateTo;

        // Filter options
        protected List<string> boroughOptions = new() { "MANHATTAN", "BROOKLYN", "QUEENS", "BRONX", "STATEN ISLAND" };
        protected List<DropDownItem> jobTypeOptions = new()
        {
            new() { Text = "A1 - Major Alteration", Value = "A1" },
            new() { Text = "A2 - Minor Alteration", Value = "A2" },
            new() { Text = "A3 - Minor Alteration", Value = "A3" },
            new() { Text = "NB - New Building", Value = "NB" },
            new() { Text = "DM - Demolition", Value = "DM" },
            new() { Text = "SG - Sign", Value = "SG" },
        };

        protected List<TradeOption> tradeOptions = new()
        {
            new() { Name = "Plumbing", Key = "Plumbing" },
            new() { Name = "Mechanical", Key = "Mechanical" },
            new() { Name = "Boiler", Key = "Boiler" },
            new() { Name = "Sprinkler", Key = "Sprinkler" },
            new() { Name = "Fire Alarm", Key = "FireAlarm" },
            new() { Name = "Standpipe", Key = "Standpipe" },
            new() { Name = "Equipment", Key = "Equipment" },
            new() { Name = "Fire Suppression", Key = "FireSuppression" },
            new() { Name = "Curb Cut", Key = "CurbCut" },
        };

        protected override async Task OnInitializedAsync()
        {
            // dateFrom = DateTime.Today.AddDays(-30); // Commented out to allow older test data to load
            await SearchPermits();
        }

        protected async Task SearchPermits()
        {
            var query = new PermitSearchQuery
            {
                SearchText = searchText,
                Boroughs = selectedBoroughs?.ToList() ?? new(),
                JobTypes = selectedJobTypes?.ToList() ?? new(),
                Trades = tradeOptions.Where(t => t.Selected).Select(t => t.Key).ToList(),
                MinCost = minCost,
                MaxCost = maxCost,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Take = 25
            };

            var (results, count) = await PermitIntelService.SearchPermits(query);
            permits = results;
            totalCount = count;
        }

        protected async Task OnFilterChanged()
        {
            // Debounce - just update state, don't auto-search
        }

        protected void ViewDetail(DobjobFiling filing)
        {
            NavigationManager.NavigateTo($"/permit-intel/detail/{filing.Id}");
        }

        protected async Task SaveAsLead(DobjobFiling filing)
        {
            try
            {
                var userId = Security.User?.Id;
                if (string.IsNullOrEmpty(userId))
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Login Required", "Please log in to save leads.");
                    return;
                }

                await PermitIntelService.SaveLead(userId, filing.Id);
                NotificationService.Notify(NotificationSeverity.Success, "Lead Saved", $"{filing.HouseNum} {filing.StreetName} added to your leads.");
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            }
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            if (args?.Value == "csv")
            {
                db_9f8bee_konxdevService.ExportDobjobFilingsToCSV(new Query { });
            }
            else if (args?.Value == "xlsx")
            {
                db_9f8bee_konxdevService.ExportDobjobFilingsToExcel(new Query { });
            }
        }

        // Helper classes
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
