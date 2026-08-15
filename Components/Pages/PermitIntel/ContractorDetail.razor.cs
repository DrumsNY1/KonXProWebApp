using Microsoft.AspNetCore.Components;
using Radzen;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class ContractorDetail
    {
        [Parameter]
        public int Id { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public KonXProWebApp.Services.ContractorIntelService ContractorIntelService { get; set; }

        protected HomeImprovementContractor contractor;
        protected ContractorRiskProfile riskProfile;
        protected List<DobjobFiling> permitFilings = new();

        protected override async Task OnInitializedAsync()
        {
            contractor = await ContractorIntelService.GetContractorById(Id);

            if (contractor != null)
            {
                permitFilings = await ContractorIntelService.GetContractorPermitFilings(contractor.LicenseNumber, contractor.BusinessName);
                
                var totalCost = permitFilings.Sum(p => p.InitialCost ?? 0m);
                riskProfile = Services.ContractorIntelService.EvaluateContractorRisk(
                    contractor,
                    activePermitCount: permitFilings.Count,
                    totalJobVolume: totalCost);
            }
        }
    }
}
