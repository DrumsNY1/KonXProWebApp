using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace KonXProWebApp.Components.Pages
{
    public partial class HighTier
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

        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard> dvwHighTierDashboards;

        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard dvwHighTierDashboard;

        protected bool isEdit = true;

        protected string search = "";


        private void NavigateTo(string url)
        {
            NavigationManager.NavigateTo(url);
        }
        protected async Task Search(ChangeEventArgs args)
        {
            search = $"{args.Value}";
            //await grid0.GoToPage(0);

            dvwHighTierDashboards = await db_9f8bee_konxdevService.GetVwHighTierDashboards(new Query { Filter = $@"i => i.Borough.Contains(@0) || i.HouseNum.Contains(@0) || i.Street.Contains(@0) || i.ProjectType.Contains(@0) || i.JobDescription.Contains(@0) || i.Neighborhood.Contains(@0)", FilterParameters = new object[] { search } });
        }
        protected override async Task OnInitializedAsync()
        {
            dvwHighTierDashboards = await db_9f8bee_konxdevService.GetVwHighTierDashboards(new Query { Filter = $@"i => i.Borough.Contains(@0) || i.HouseNum.Contains(@0) || i.Street.Contains(@0) || i.ProjectType.Contains(@0) || i.JobDescription.Contains(@0) || i.Neighborhood.Contains(@0)", FilterParameters = new object[] { search } });
        }



        protected async Task FormSubmit()
        {
            try
            {

            }
            catch (Exception ex)
            {
                errorVisible = true;
            }
        }

        protected async Task CancelButtonClick(MouseEventArgs args)
        {

        }


        private bool IsFilterVisible = false;
    }


}