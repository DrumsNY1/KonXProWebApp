using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using KonXProWebApp.Models.db_9f8bee_konxdev;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class PermitDetail
    {
        [Parameter]
        public int Id { get; set; }

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

        protected DobjobFiling filing;
        protected bool isLeadSaved = false;
        protected IEnumerable<HpdViolation> hpdViolations = Enumerable.Empty<HpdViolation>();
        protected IEnumerable<DobViolation> dobViolations = Enumerable.Empty<DobViolation>();
        protected IEnumerable<ServiceRequest311> complaints311 = Enumerable.Empty<ServiceRequest311>();

        protected override async Task OnInitializedAsync()
        {
            filing = await PermitIntelService.GetPermitById(Id);

            if (filing != null)
            {
                int dobCount = 0, hpdCount = 0, hpdClassCCount = 0;
                if (!string.IsNullOrEmpty(filing.Bin))
                {
                    hpdViolations = await PermitIntelService.GetHpdViolationsByBin(filing.Bin);
                    dobViolations = await PermitIntelService.GetDobViolationsByBin(filing.Bin);
                    (dobCount, hpdCount, hpdClassCCount) = await PermitIntelService.GetViolationSummaryByBin(filing.Bin);
                }
                
                var bbl = KonXProWebApp.Services.PermitIntelService.GetBblFromFiling(filing);
                int velocity = 0;
                if (!string.IsNullOrEmpty(bbl))
                {
                    complaints311 = await PermitIntelService.Get311ComplaintsByBbl(bbl);
                    velocity = await PermitIntelService.Get311ComplaintVelocity(bbl);
                }
                filing.LeadScore = KonXProWebApp.Services.PermitIntelService.ScorePermit(filing, velocity, dobCount, hpdClassCCount);

                var userId = Security.User?.Id;
                if (!string.IsNullOrEmpty(userId))
                {
                    var leads = await PermitIntelService.GetSavedLeads(userId);
                    isLeadSaved = leads.Any(l => l.DobjobFilingId == Id);
                }
            }
        }

        protected async Task ToggleSaveLead()
        {
            var userId = Security.User?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            if (isLeadSaved)
            {
                var leads = await PermitIntelService.GetSavedLeads(userId);
                var lead = leads.FirstOrDefault(l => l.DobjobFilingId == Id);
                if (lead != null)
                {
                    await PermitIntelService.DeleteLead(lead.Id);
                    isLeadSaved = false;
                    NotificationService.Notify(NotificationSeverity.Info, "Removed", "Lead removed from your pipeline.");
                }
            }
            else
            {
                await PermitIntelService.SaveLead(userId, Id);
                isLeadSaved = true;
                NotificationService.Notify(NotificationSeverity.Success, "Saved", "Lead added to your pipeline.");
            }
        }
    }
}
