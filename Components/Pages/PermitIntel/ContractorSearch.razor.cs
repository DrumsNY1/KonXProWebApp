using Microsoft.AspNetCore.Components;
using Radzen;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class ContractorSearch
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public KonXProWebApp.Services.ContractorIntelService ContractorIntelService { get; set; }

        protected IEnumerable<HomeImprovementContractor> contractors;
        protected int totalCount = 0;
        protected int activeCount = 0;
        protected bool isLoading = false;

        // Filter state
        protected string searchText = "";
        protected string selectedBorough;
        protected string selectedStatus;

        protected List<string> boroughOptions = new() { "MANHATTAN", "BROOKLYN", "QUEENS", "BRONX", "STATEN ISLAND" };
        protected List<string> statusOptions = new() { "Active", "Expired", "Inactive", "Suspended" };

        protected override async Task OnInitializedAsync()
        {
            await SearchContractors();
        }

        protected async Task SearchContractors()
        {
            isLoading = true;
            try
            {
                var query = new ContractorSearchQuery
                {
                    SearchText = searchText,
                    Boroughs = !string.IsNullOrEmpty(selectedBorough) ? new() { selectedBorough } : new(),
                    LicenseStatuses = !string.IsNullOrEmpty(selectedStatus) ? new() { selectedStatus } : new(),
                    Take = 50
                };

                var (results, count) = await ContractorIntelService.SearchContractors(query);
                contractors = results;
                totalCount = count;
                activeCount = results.Count(c => c.LicenseStatus?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true);
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
            selectedStatus = null;
            await SearchContractors();
        }

        protected void ViewContractorDetail(int id)
        {
            NavigationManager.NavigateTo($"/permit-intel/contractor/{id}");
        }
    }
}
