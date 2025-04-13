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
    public partial class EcbViolations
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

        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> ecbViolations;

        protected RadzenDataGrid<KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation> grid0;
        protected bool isEdit = true;

        protected string search = "";

        protected async Task Search(ChangeEventArgs args)
        {
            search = $"{args.Value}";

            await grid0.GoToPage(0);

            ecbViolations = await db_9f8bee_konxdevService.GetEcbViolations(new Query { Filter = $@"i => i.IsnDobBisExtract.Contains(@0) || i.EcbViolationNumber.Contains(@0) || i.EcbViolationStatus.Contains(@0) || i.Bin.Contains(@0) || i.Block.Contains(@0) || i.Lot.Contains(@0) || i.HearingTime.Contains(@0) || i.Severity.Contains(@0) || i.ViolationType.Contains(@0) || i.RespondentName.Contains(@0) || i.RespondentHouseNumber.Contains(@0) || i.RespondentStreet.Contains(@0) || i.RespondentCity.Contains(@0) || i.RespondentZip.Contains(@0) || i.ViolationDescription.Contains(@0) || i.InfractionCode1.Contains(@0) || i.SectionLawDescription1.Contains(@0) || i.AggravatedLevel.Contains(@0) || i.HearingStatus.Contains(@0) || i.CertificationStatus.Contains(@0)", FilterParameters = new object[] { search } });
        }
        protected override async Task OnInitializedAsync()
        {
            ecbViolations = await db_9f8bee_konxdevService.GetEcbViolations(new Query { Filter = $@"i => i.IsnDobBisExtract.Contains(@0) || i.EcbViolationNumber.Contains(@0) || i.EcbViolationStatus.Contains(@0) || i.Bin.Contains(@0) || i.Block.Contains(@0) || i.Lot.Contains(@0) || i.HearingTime.Contains(@0) || i.Severity.Contains(@0) || i.ViolationType.Contains(@0) || i.RespondentName.Contains(@0) || i.RespondentHouseNumber.Contains(@0) || i.RespondentStreet.Contains(@0) || i.RespondentCity.Contains(@0) || i.RespondentZip.Contains(@0) || i.ViolationDescription.Contains(@0) || i.InfractionCode1.Contains(@0) || i.SectionLawDescription1.Contains(@0) || i.AggravatedLevel.Contains(@0) || i.HearingStatus.Contains(@0) || i.CertificationStatus.Contains(@0)", FilterParameters = new object[] { search } });
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await grid0.SelectRow(null);
            isEdit = false;
            ecbViolation = new KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation();
        }

        protected async Task EditRow(KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation args)
        {
            isEdit = true;
            ecbViolation = args;
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation ecbViolation)
        {
            try
            {
                if (await DialogService.Confirm("Are you sure you want to delete this record?") == true)
                {
                    var deleteResult = await db_9f8bee_konxdevService.DeleteEcbViolation(ecbViolation.Id);

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
                    Detail = $"Unable to delete EcbViolation"
                });
            }
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            if (args?.Value == "csv")
            {
                await db_9f8bee_konxdevService.ExportEcbViolationsToCSV(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "EcbViolations");
            }

            if (args == null || args.Value == "xlsx")
            {
                await db_9f8bee_konxdevService.ExportEcbViolationsToExcel(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "EcbViolations");
            }
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.EcbViolation ecbViolation;

        protected async Task FormSubmit()
        {
            try
            {
                var result = isEdit ? await db_9f8bee_konxdevService.UpdateEcbViolation(ecbViolation.Id, ecbViolation) : await db_9f8bee_konxdevService.CreateEcbViolation(ecbViolation);

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