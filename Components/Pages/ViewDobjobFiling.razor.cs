using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace KonXProWebApp.Components.Pages
{
    public partial class ViewDobjobFiling
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

        //[Parameter]
        //public int Id { get; set; }

        [Parameter]
        public int jobNum { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Extract the job number from the query string
                var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var jobNumString = query["jobnum"];

                if (string.IsNullOrEmpty(jobNumString) || !int.TryParse(jobNumString, out int jobNum))
                {
                    throw new ArgumentException("Job number is missing or invalid in the URL.");
                }

                // Retrieve the record from the database using the extracted jobNum
                dobjobFiling = await db_9f8bee_konxdevService.GetDobjobFilingById(jobNum);

                if (dobjobFiling == null)
                {
                    throw new Exception($"No record found for Job Number: {jobNum}");
                }

                // Retrieve subscription and load violations if applicable
                var userId = Security.User.Id;
                ActiveSubscription = await PermitIntelService.GetActiveSubscription(userId);

                if (HasDobAccess)
                {
                    DobViolations = await PermitIntelService.GetDobViolationsByBin(dobjobFiling.Bin);
                }

                if (HasHpdAccess)
                {
                    HpdViolations = await PermitIntelService.GetHpdViolationsByBin(dobjobFiling.Bin);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to retrieve record: {ex.Message}");
            }
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling dobjobFiling;
        
        [Inject]
        protected KonXProWebApp.Services.PermitIntelService PermitIntelService { get; set; }

        protected KonXProWebApp.Models.PermitIntel.Subscription ActiveSubscription;
        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> DobViolations;
        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.HpdViolation> HpdViolations;

        protected bool HasDobAccess => ActiveSubscription?.Tier == "ComplianceAlerts" || ActiveSubscription?.Tier == "LandlordCompliance";
        protected bool HasHpdAccess => ActiveSubscription?.Tier == "LandlordCompliance";

        [Inject]
        protected SecurityService Security { get; set; }


        protected async Task CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }
        protected async Task<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> GetDobjobFilingFromUrl(string url)
        {
            try
            {
                // Extract the job number from the query string
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var jobNumString = query["jobnum"];

                if (string.IsNullOrEmpty(jobNumString) || !int.TryParse(jobNumString, out int jobNum))
                {
                    throw new ArgumentException("Job number is missing or invalid in the URL.");
                }

                // Retrieve the record from the database
                var dobjobFiling = await db_9f8bee_konxdevService.GetDobjobFilingById(jobNum);

                if (dobjobFiling == null)
                {
                    throw new Exception($"No record found for Job Number: {jobNum}");
                }

                return dobjobFiling;
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to retrieve record: {ex.Message}");
                return null;
            }
        }
    }
}