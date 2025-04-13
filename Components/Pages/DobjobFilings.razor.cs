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
    public partial class DobjobFilings
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

        protected IEnumerable<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> dobjobFilings;

        protected RadzenDataGrid<KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling> grid0;
        protected bool isEdit = true;

        protected string search = "";

        protected async Task Search(ChangeEventArgs args)
        {
            search = $"{args.Value}";

            await grid0.GoToPage(0);

            dobjobFilings = await db_9f8bee_konxdevService.GetDobjobFilings(new Query { Filter = $@"i => i.DocNum.Contains(@0) || i.Borough.Contains(@0) || i.HouseNum.Contains(@0) || i.StreetName.Contains(@0) || i.Block.Contains(@0) || i.Lot.Contains(@0) || i.Bin.Contains(@0) || i.JobType.Contains(@0) || i.JobStatus.Contains(@0) || i.JobStatusDescrp.Contains(@0) || i.BuildingType.Contains(@0) || i.CommunityBoard.Contains(@0) || i.Cluster.Contains(@0) || i.Landmarked.Contains(@0) || i.AdultEstab.Contains(@0) || i.LoftBoard.Contains(@0) || i.CityOwned.Contains(@0) || i.Littlee.Contains(@0) || i.Pcfiled.Contains(@0) || i.EFilingFiled.Contains(@0) || i.Plumbing.Contains(@0) || i.Mechanical.Contains(@0) || i.Boiler.Contains(@0) || i.FuelBurning.Contains(@0) || i.FuelStorage.Contains(@0) || i.Standpipe.Contains(@0) || i.Sprinkler.Contains(@0) || i.FireAlarm.Contains(@0) || i.Equipment.Contains(@0) || i.FireSuppression.Contains(@0) || i.CurbCut.Contains(@0) || i.Other.Contains(@0) || i.OtherDescription.Contains(@0) || i.ApplicantsFirstName.Contains(@0) || i.ApplicantsLastName.Contains(@0) || i.ApplicantProfessionalTitle.Contains(@0) || i.ApplicantLicenseNum.Contains(@0) || i.ProfessionalCert.Contains(@0) || i.FeeStatus.Contains(@0) || i.ExistingZoningSqft.Contains(@0) || i.ProposedZoningSqft.Contains(@0) || i.HorizontalEnlrgmt.Contains(@0) || i.VerticalEnlrgmt.Contains(@0) || i.EnlargementSqfootage.Contains(@0) || i.StreetFrontage.Contains(@0) || i.ExistingNoofStories.Contains(@0) || i.ProposedNoofStories.Contains(@0) || i.ExistingHeight.Contains(@0) || i.ProposedHeight.Contains(@0) || i.ExistingDwellingUnits.Contains(@0) || i.ProposedDwellingUnits.Contains(@0) || i.ExistingOccupancy.Contains(@0) || i.ProposedOccupancy.Contains(@0) || i.SiteFill.Contains(@0) || i.ZoningDist1.Contains(@0) || i.ZoningDist2.Contains(@0) || i.ZoningDist3.Contains(@0) || i.SpecialDistrict1.Contains(@0) || i.SpecialDistrict2.Contains(@0) || i.OwnerType.Contains(@0) || i.NonProfit.Contains(@0) || i.OwnersFirstName.Contains(@0) || i.OwnersLastName.Contains(@0) || i.OwnersBusinessName.Contains(@0) || i.OwnersHouseNumber.Contains(@0) || i.OwnersHouseStreetName.Contains(@0) || i.City.Contains(@0) || i.State.Contains(@0) || i.Zip.Contains(@0) || i.OwnersPhone.Contains(@0) || i.JobDescription.Contains(@0) || i.Jobs1no.Contains(@0) || i.Totalconstructionfloorarea.Contains(@0) || i.Withdrawalflag.Contains(@0) || i.Specialactionstatus.Contains(@0) || i.Specialactiondate.Contains(@0) || i.Buildingclass.Contains(@0) || i.Jobnogoodcount.Contains(@0) || i.Gislatitude.Contains(@0) || i.Gislongitude.Contains(@0) || i.Giscouncildistrict.Contains(@0) || i.Giscensustract.Contains(@0) || i.Gisntaname.Contains(@0) || i.Gisbin.Contains(@0)", FilterParameters = new object[] { search } });
        }
        protected override async Task OnInitializedAsync()
        {
            dobjobFilings = await db_9f8bee_konxdevService.GetDobjobFilings(new Query { Filter = $@"i => i.DocNum.Contains(@0) || i.Borough.Contains(@0) || i.HouseNum.Contains(@0) || i.StreetName.Contains(@0) || i.Block.Contains(@0) || i.Lot.Contains(@0) || i.Bin.Contains(@0) || i.JobType.Contains(@0) || i.JobStatus.Contains(@0) || i.JobStatusDescrp.Contains(@0) || i.BuildingType.Contains(@0) || i.CommunityBoard.Contains(@0) || i.Cluster.Contains(@0) || i.Landmarked.Contains(@0) || i.AdultEstab.Contains(@0) || i.LoftBoard.Contains(@0) || i.CityOwned.Contains(@0) || i.Littlee.Contains(@0) || i.Pcfiled.Contains(@0) || i.EFilingFiled.Contains(@0) || i.Plumbing.Contains(@0) || i.Mechanical.Contains(@0) || i.Boiler.Contains(@0) || i.FuelBurning.Contains(@0) || i.FuelStorage.Contains(@0) || i.Standpipe.Contains(@0) || i.Sprinkler.Contains(@0) || i.FireAlarm.Contains(@0) || i.Equipment.Contains(@0) || i.FireSuppression.Contains(@0) || i.CurbCut.Contains(@0) || i.Other.Contains(@0) || i.OtherDescription.Contains(@0) || i.ApplicantsFirstName.Contains(@0) || i.ApplicantsLastName.Contains(@0) || i.ApplicantProfessionalTitle.Contains(@0) || i.ApplicantLicenseNum.Contains(@0) || i.ProfessionalCert.Contains(@0) || i.FeeStatus.Contains(@0) || i.ExistingZoningSqft.Contains(@0) || i.ProposedZoningSqft.Contains(@0) || i.HorizontalEnlrgmt.Contains(@0) || i.VerticalEnlrgmt.Contains(@0) || i.EnlargementSqfootage.Contains(@0) || i.StreetFrontage.Contains(@0) || i.ExistingNoofStories.Contains(@0) || i.ProposedNoofStories.Contains(@0) || i.ExistingHeight.Contains(@0) || i.ProposedHeight.Contains(@0) || i.ExistingDwellingUnits.Contains(@0) || i.ProposedDwellingUnits.Contains(@0) || i.ExistingOccupancy.Contains(@0) || i.ProposedOccupancy.Contains(@0) || i.SiteFill.Contains(@0) || i.ZoningDist1.Contains(@0) || i.ZoningDist2.Contains(@0) || i.ZoningDist3.Contains(@0) || i.SpecialDistrict1.Contains(@0) || i.SpecialDistrict2.Contains(@0) || i.OwnerType.Contains(@0) || i.NonProfit.Contains(@0) || i.OwnersFirstName.Contains(@0) || i.OwnersLastName.Contains(@0) || i.OwnersBusinessName.Contains(@0) || i.OwnersHouseNumber.Contains(@0) || i.OwnersHouseStreetName.Contains(@0) || i.City.Contains(@0) || i.State.Contains(@0) || i.Zip.Contains(@0) || i.OwnersPhone.Contains(@0) || i.JobDescription.Contains(@0) || i.Jobs1no.Contains(@0) || i.Totalconstructionfloorarea.Contains(@0) || i.Withdrawalflag.Contains(@0) || i.Specialactionstatus.Contains(@0) || i.Specialactiondate.Contains(@0) || i.Buildingclass.Contains(@0) || i.Jobnogoodcount.Contains(@0) || i.Gislatitude.Contains(@0) || i.Gislongitude.Contains(@0) || i.Giscouncildistrict.Contains(@0) || i.Giscensustract.Contains(@0) || i.Gisntaname.Contains(@0) || i.Gisbin.Contains(@0)", FilterParameters = new object[] { search } });
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await grid0.SelectRow(null);
            isEdit = false;
            dobjobFiling = new KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling();
        }

        protected async Task EditRow(KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling args)
        {
            isEdit = true;
            dobjobFiling = args;
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling dobjobFiling)
        {
            try
            {
                if (await DialogService.Confirm("Are you sure you want to delete this record?") == true)
                {
                    var deleteResult = await db_9f8bee_konxdevService.DeleteDobjobFiling(dobjobFiling.Id);

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
                    Detail = $"Unable to delete DobjobFiling"
                });
            }
        }

        protected async Task ExportClick(RadzenSplitButtonItem args)
        {
            if (args?.Value == "csv")
            {
                await db_9f8bee_konxdevService.ExportDobjobFilingsToCSV(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "DobjobFilings");
            }

            if (args == null || args.Value == "xlsx")
            {
                await db_9f8bee_konxdevService.ExportDobjobFilingsToExcel(new Query
                {
                    Filter = $@"{(string.IsNullOrEmpty(grid0.Query.Filter)? "true" : grid0.Query.Filter)}",
                    OrderBy = $"{grid0.Query.OrderBy}",
                    Expand = "",
                    Select = string.Join(",", grid0.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property)).Select(c => c.Property.Contains(".") ? c.Property + " as " + c.Property.Replace(".", "") : c.Property))
                }, "DobjobFilings");
            }
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling dobjobFiling;

        protected async Task FormSubmit()
        {
            try
            {
                var result = isEdit ? await db_9f8bee_konxdevService.UpdateDobjobFiling(dobjobFiling.Id, dobjobFiling) : await db_9f8bee_konxdevService.CreateDobjobFiling(dobjobFiling);

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