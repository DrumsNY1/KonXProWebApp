using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace KonXProWebApp.Components.Pages
{
    public partial class VwHighTierDashboards
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

        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard> vwHighTierDashboards;

        protected RadzenDataGrid<KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard> grid0;
        protected bool isEdit = true;

        protected string search = "";

        protected async Task Search(ChangeEventArgs args)
        {
            search = $"{args.Value}";

            await grid0.GoToPage(0);

            vwHighTierDashboards = await db_9f8bee_konxdevService.GetVwHighTierDashboards(new Query { Filter = $@"i => i.Borough.Contains(@0) || i.HouseNum.Contains(@0) || i.Street.Contains(@0) || i.ProjectType.Contains(@0) || i.JobDescription.Contains(@0) || i.Neighborhood.Contains(@0)", FilterParameters = new object[] { search } });
        }
        protected override async Task OnInitializedAsync()
        {
            vwHighTierDashboards = await db_9f8bee_konxdevService.GetVwHighTierDashboards(new Query { Filter = $@"i => i.Borough.Contains(@0) || i.HouseNum.Contains(@0) || i.Street.Contains(@0) || i.ProjectType.Contains(@0) || i.JobDescription.Contains(@0) || i.Neighborhood.Contains(@0)", FilterParameters = new object[] { search } });
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            if (args?.Value == "csv")
            {
                await db_9f8bee_konxdevService.ExportVwHighTierDashboardsToCSV(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "VwHighTierDashboards");
            }

            if (args == null || args.Value == "xlsx")
            {
                await db_9f8bee_konxdevService.ExportVwHighTierDashboardsToExcel(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "VwHighTierDashboards");
            }
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.VwHighTierDashboard vwHighTierDashboard;

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
    }
}