using Microsoft.AspNetCore.Components;
using Radzen;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class PropertyCompliance
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public KonXProWebApp.Services.ComplianceIntelService ComplianceIntelService { get; set; }

        protected IEnumerable<DobViolation> dobViolations;
        protected IEnumerable<HpdViolation> hpdViolations;

        protected int totalDobCount = 0;
        protected int totalHpdCount = 0;
        protected int classCCount = 0;
        protected bool isLoading = false;

        // Filter state
        protected string searchText = "";
        protected string selectedBorough;
        protected string selectedSeverity = "All";

        protected List<string> boroughOptions = new() { "MANHATTAN", "BROOKLYN", "QUEENS", "BRONX", "STATEN ISLAND" };
        protected List<string> severityOptions = new() { "All", "A", "B", "C" };

        protected override async Task OnInitializedAsync()
        {
            await SearchViolations();
        }

        protected async Task SearchViolations()
        {
            isLoading = true;
            try
            {
                var query = new PropertyComplianceQuery
                {
                    SearchText = searchText,
                    Borough = selectedBorough,
                    SeverityClass = selectedSeverity,
                    Take = 50
                };

                var (dobResults, dobTotal) = await ComplianceIntelService.SearchDobViolations(query);
                var (hpdResults, hpdTotal) = await ComplianceIntelService.SearchHpdViolations(query);

                dobViolations = dobResults;
                hpdViolations = hpdResults;
                totalDobCount = dobTotal;
                totalHpdCount = hpdTotal;
                classCCount = hpdResults.Count(v => v.Class == "C");
            }
            catch (Exception ex)
            {
                NotificationService?.Notify(NotificationSeverity.Error, "Search Error", ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        protected async Task ResetFilters()
        {
            searchText = "";
            selectedBorough = null;
            selectedSeverity = "All";
            await SearchViolations();
        }
    }
}
