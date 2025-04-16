using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace KonXProWebApp.Components.Pages
{
    public partial class AgentDetails
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

        class DealsDataItem
        {
            public string PropertyType { get; set; }
            public double Deals { get; set; }
        }

        DealsDataItem[] agentDeals = new DealsDataItem[] {
            new DealsDataItem {
                PropertyType = "Apartment",
                Deals = 80
            },
            new DealsDataItem {
                PropertyType = "House",
                Deals = 20
            },
            new DealsDataItem {
                PropertyType = "Villa",
                Deals = 30
            },
            new DealsDataItem {
                PropertyType = "Other",
                Deals = 40
            },
        };

        class DealTypeDataItem
        {
            public string DealType { get; set; }
            public double Deals { get; set; }
        }

        DealTypeDataItem[] dealTypes = new DealTypeDataItem[] {
            new DealTypeDataItem {
                DealType = "Rent",
                Deals = 60
            },
            new DealTypeDataItem {
                DealType = "Sale",
                Deals = 110
            },
        };

        class DealsByMonth
        {
            public string Month { get; set; }
            public double Deals { get; set; }
        }

        DealsByMonth[] dealsByMonth = new DealsByMonth[] {
            new DealsByMonth {
                Month = "Jan",
                Deals = 30
            },
            new DealsByMonth {
                Month = "Feb",
                Deals = 12
            },
            new DealsByMonth {
                Month = "Mar",
                Deals = 20
            },
            new DealsByMonth {
                Month = "Apr",
                Deals = 5
            },
            new DealsByMonth {
                Month = "May",
                Deals = 14
            },
        };

        [Inject]
        protected SecurityService Security { get; set; }
    }
}