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

        protected IEnumerable<SavedLead> leads;
        protected IEnumerable<SavedLead> filteredLeads;
        protected RadzenDataGrid<SavedLead> grid0;
        protected string selectedStatus = "All";

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

            leads = await PermitIntelService.GetSavedLeads(userId);
            ApplyStatusFilter();
        }

        protected void FilterByStatus(string status)
        {
            selectedStatus = status;
            ApplyStatusFilter();
        }

        private void ApplyStatusFilter()
        {
            filteredLeads = selectedStatus == "All"
                ? leads
                : leads?.Where(l => l.Status == selectedStatus);
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
        }

        protected async Task UpdateNotes(SavedLead lead)
        {
            await PermitIntelService.UpdateLeadStatus(lead.Id, lead.Status, lead.Notes);
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
