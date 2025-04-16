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
    public partial class DobViolations
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

        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> dobViolations;

        protected RadzenDataGrid<KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation> grid0;
        protected bool isEdit = true;

        protected string search = "";

        protected async Task Search(ChangeEventArgs args)
        {
            search = $"{args.Value}";

            await grid0.GoToPage(0);

            dobViolations = await db_9f8bee_konxdevService.GetDobViolations(new Query { Filter = $@"i => i.IsnDobBisViol.Contains(@0) || i.Bin.Contains(@0) || i.Block.Contains(@0) || i.Lot.Contains(@0) || i.ViolationTypeCode.Contains(@0) || i.ViolationNumber.Contains(@0) || i.HouseNumber.Contains(@0) || i.Street.Contains(@0) || i.Description.Contains(@0) || i.Number.Contains(@0) || i.ViolationCategory.Contains(@0) || i.ViolationType.Contains(@0)", FilterParameters = new object[] { search } });
        }
        protected override async Task OnInitializedAsync()
        {
            dobViolations = await db_9f8bee_konxdevService.GetDobViolations(new Query { Filter = $@"i => i.IsnDobBisViol.Contains(@0) || i.Bin.Contains(@0) || i.Block.Contains(@0) || i.Lot.Contains(@0) || i.ViolationTypeCode.Contains(@0) || i.ViolationNumber.Contains(@0) || i.HouseNumber.Contains(@0) || i.Street.Contains(@0) || i.Description.Contains(@0) || i.Number.Contains(@0) || i.ViolationCategory.Contains(@0) || i.ViolationType.Contains(@0)", FilterParameters = new object[] { search } });
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await grid0.SelectRow(null);
            isEdit = false;
            dobViolation = new KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation();
        }

        protected async Task EditRow(KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation args)
        {
            isEdit = true;
            dobViolation = args;
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation dobViolation)
        {
            try
            {
                if (await DialogService.Confirm("Are you sure you want to delete this record?") == true)
                {
                    var deleteResult = await db_9f8bee_konxdevService.DeleteDobViolation(dobViolation.Id);

                    if (deleteResult != null)
                    {
                        await grid0.Reload();
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = $"Error",
                    Detail = $"Unable to delete DobViolation"
                });
            }
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            if (args?.Value == "csv")
            {
                await db_9f8bee_konxdevService.ExportDobViolationsToCSV(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "DobViolations");
            }

            if (args == null || args.Value == "xlsx")
            {
                await db_9f8bee_konxdevService.ExportDobViolationsToExcel(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "DobViolations");
            }
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation dobViolation;

        [Inject]
        protected SecurityService Security { get; set; }

        protected async Task FormSubmit()
        {
            try
            {
                var result = isEdit ? await db_9f8bee_konxdevService.UpdateDobViolation(dobViolation.Id, dobViolation) : await db_9f8bee_konxdevService.CreateDobViolation(dobViolation);

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