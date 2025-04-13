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
    public partial class AddDobViolation
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

        protected override async Task OnInitializedAsync()
        {
            dobViolation = new KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation();
        }
        protected bool errorVisible;
        protected KonXProWebApp.Models.db_9f8bee_konxdev.DobViolation dobViolation;

        protected async Task FormSubmit()
        {
            try
            {
                await db_9f8bee_konxdevService.CreateDobViolation(dobViolation);
                DialogService.Close(dobViolation);
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