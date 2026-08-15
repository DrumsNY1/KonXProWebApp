using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class MyLeads
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected SecurityService Security { get; set; }

        [Inject]
        public KonXProWebApp.Services.PermitIntelService PermitIntelService { get; set; }

        protected IEnumerable<SavedLead> leads;
        protected IEnumerable<SavedLead> filteredLeads;
        protected IList<SavedLead> selectedLeads = new List<SavedLead>();
        protected RadzenDataGrid<SavedLead> grid0;

        protected string selectedStatus = "All";
        protected string searchText = "";

        protected int totalLeadsCount = 0;
        protected int hotLeadsCount = 0;
        protected decimal totalPipelineValue = 0m;
        protected int activeOpportunitiesCount = 0;

        protected List<string> statusOptions = new() { "All", "New", "Contacted", "Quoted", "Won", "Lost" };
        protected List<string> leadStatusOptions = new() { "New", "Contacted", "Quoted", "Won", "Lost" };

        protected override async Task OnInitializedAsync()
        {
            await LoadLeads();
        }

        private async Task LoadLeads()
        {
            var userId = Security.User?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            var result = await PermitIntelService.GetSavedLeads(userId);
            leads = result;

            // Compute statistics
            totalLeadsCount = leads?.Count() ?? 0;
            hotLeadsCount = leads?.Count(l => l.DobjobFiling != null && l.DobjobFiling.LeadScore >= 4) ?? 0;
            totalPipelineValue = leads?.Sum(l => l.DobjobFiling?.InitialCost ?? 0m) ?? 0m;
            activeOpportunitiesCount = leads?.Count(l => l.Status == "Contacted" || l.Status == "Quoted") ?? 0;

            ApplyFilter();
        }

        protected void FilterByStatus(string status)
        {
            selectedStatus = status;
            ApplyFilter();
        }

        protected void ApplyFilter()
        {
            var query = leads ?? Enumerable.Empty<SavedLead>();

            if (selectedStatus != "All")
            {
                query = query.Where(l => l.Status == selectedStatus);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.Trim().ToLower();
                query = query.Where(l =>
                    (l.DobjobFiling != null && $"{l.DobjobFiling.HouseNum} {l.DobjobFiling.StreetName}".ToLower().Contains(search)) ||
                    (l.DobjobFiling != null && l.DobjobFiling.Borough != null && l.DobjobFiling.Borough.ToLower().Contains(search)) ||
                    (l.Notes != null && l.Notes.ToLower().Contains(search)));
            }

            filteredLeads = query.ToList();
        }

        protected int GetStatusCount(string status)
        {
            if (leads == null) return 0;
            return status == "All" ? leads.Count() : leads.Count(l => l.Status == status);
        }

        protected async Task UpdateStatus(SavedLead lead)
        {
            await PermitIntelService.UpdateLeadStatus(lead.Id, lead.Status);
            NotificationService.Notify(NotificationSeverity.Success, "Updated", $"Lead status changed to {lead.Status}.");
            await LoadLeads();
        }

        protected async Task UpdateNotes(SavedLead lead)
        {
            await PermitIntelService.UpdateLeadStatus(lead.Id, lead.Status, lead.Notes);
        }

        protected async Task BulkSetStatus(string newStatus)
        {
            if (selectedLeads == null || !selectedLeads.Any()) return;

            var ids = selectedLeads.Select(l => l.Id).ToList();
            await PermitIntelService.BulkUpdateLeadStatus(ids, newStatus);
            NotificationService.Notify(NotificationSeverity.Success, "Bulk Update", $"Updated {ids.Count} lead(s) status to {newStatus}.");
            selectedLeads.Clear();
            await LoadLeads();
        }

        protected async Task BulkDeleteLeads()
        {
            if (selectedLeads == null || !selectedLeads.Any()) return;

            var confirmed = await DialogService.Confirm(
                $"Remove {selectedLeads.Count} lead(s) from your pipeline?",
                "Bulk Delete Leads",
                new ConfirmOptions { OkButtonText = "Delete All", CancelButtonText = "Cancel" });

            if (confirmed == true)
            {
                foreach (var lead in selectedLeads.ToList())
                {
                    await PermitIntelService.DeleteLead(lead.Id);
                }
                selectedLeads.Clear();
                await LoadLeads();
                NotificationService.Notify(NotificationSeverity.Info, "Deleted", "Selected leads removed from pipeline.");
            }
        }

        protected void ToggleSelectAll(bool? value)
        {
            if (value == true && filteredLeads != null)
            {
                selectedLeads = filteredLeads.ToList();
            }
            else
            {
                selectedLeads.Clear();
            }
        }

        protected void ToggleSelect(SavedLead lead, bool? value)
        {
            if (value == true)
            {
                if (!selectedLeads.Contains(lead)) selectedLeads.Add(lead);
            }
            else
            {
                selectedLeads.Remove(lead);
            }
        }

        protected async Task ExportToCsv()
        {
            var dataToExport = filteredLeads ?? leads;
            var csvBytes = Services.PermitIntelService.ExportLeadsToCsv(dataToExport);
            var base64 = Convert.ToBase64String(csvBytes);
            var fileName = $"my_leads_pipeline_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", base64, fileName, "text/csv");
            NotificationService.Notify(NotificationSeverity.Success, "Export Complete", $"Exported {dataToExport.Count()} lead(s) to CSV.");
        }

        protected async Task RemoveLead(SavedLead lead)
        {
            var confirmed = await DialogService.Confirm(
                "Remove this lead from your pipeline?",
                "Remove Lead",
                new ConfirmOptions { OkButtonText = "Remove", CancelButtonText = "Cancel" });

            if (confirmed == true)
            {
                await PermitIntelService.DeleteLead(lead.Id);
                await LoadLeads();
                NotificationService.Notify(NotificationSeverity.Info, "Removed", "Lead removed from your pipeline.");
            }
        }
    }
}
