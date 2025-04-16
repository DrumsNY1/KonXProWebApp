using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace KonXProWebApp.Components.Pages
{
    public partial class EditDobjobFiling
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

        [Parameter]
        public string JobNum { get; set; }

        protected override async Task OnInitializedAsync()
        {
            dobjobFiling = await db_9f8bee_konxdevService.GetDobjobFilingByJobId(JobNum);
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.DobjobFiling dobjobFiling;

        [Inject]
        protected SecurityService Security { get; set; }

        protected async Task FormSubmit()
        {
            try
            {
                await db_9f8bee_konxdevService.UpdateDobjobFiling(JobNum, dobjobFiling);
                DialogService.Close(dobjobFiling);
            }
            catch (Exception ex)
            {
                errorVisible = true;
            }
        }

        protected async Task CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }
    }
}