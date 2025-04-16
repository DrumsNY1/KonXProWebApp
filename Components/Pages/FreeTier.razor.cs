using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace KonXProWebApp.Components.Pages
{
    public partial class FreeTier
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

        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.VwFreeTierDashboard> dvwFreeTierDashboards;

        protected RadzenDataGrid<KonXProWebApp.Models.db_9f8bee_konxdev.VwFreeTierDashboard> grid0;
        protected bool isEdit = true;

        protected string search = "";


        private void NavigateTo(string url)
        {
            NavigationManager.NavigateTo(url);
        }

        protected async Task Search(ChangeEventArgs args)
        {
            search = $"{args.Value}";

            await grid0.GoToPage(0);

            dvwFreeTierDashboards = await db_9f8bee_konxdevService.GetVwFreeTierDashboards(new Query { Filter = $@"i => i.Borough.Contains(@0) ||  i.Street.Contains(@0) || i.ProjectType.Contains(@0) || i.JobDescription.Contains(@0) || i.Neighborhood.Contains(@0)", FilterParameters = new object[] { search } });
        }
        protected override async Task OnInitializedAsync()
        {
            dvwFreeTierDashboards = await db_9f8bee_konxdevService.GetVwFreeTierDashboards(new Query { Filter = $@"i => i.Borough.Contains(@0) ||  i.Street.Contains(@0) || i.ProjectType.Contains(@0) || i.JobDescription.Contains(@0) || i.Neighborhood.Contains(@0)", FilterParameters = new object[] { search } });
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            //if (args?.Value == "csv")
            //{
            //    await db_9f8bee_konxdevService.ExportdvwFreeTierDashboardsToCSV(new Query
            //    {
            //        Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter) ? "true" : grid0.Query.Filter)}",
            //        OrderBy = $"{grid0.Query.OrderBy}",
            //        Expand = "",
            //        Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
            //    }, "VwBasicTierDashboards");
            //}

            //if (args == null || args.Value == "xlsx")
            //{
            //    await db_9f8bee_konxdevService.ExportdvwFreeTierDashboardsToExcel(new Query
            //    {
            //        Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter) ? "true" : grid0.Query.Filter)}",
            //        OrderBy = $"{grid0.Query.OrderBy}",
            //        Expand = "",
            //        Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
            //    }, "VwBasicTierDashboards");
            //}
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.VwFreeTierDashboard vwFreeTierDashboard;

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

        [Inject]
        protected SecurityService Security { get; set; }
    }


}