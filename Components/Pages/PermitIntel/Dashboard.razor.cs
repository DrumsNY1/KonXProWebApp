using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class Dashboard
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

        [Inject]
        protected SecurityService Security { get; set; }

        [Inject]
        public KonXProWebApp.Services.PermitIntelService PermitIntelService { get; set; }

        protected PermitDashboardStats stats;
        protected KonXProWebApp.Models.PermitIntel.Subscription currentSubscription;
        protected List<BoroughChartItem> boroughData = new();
        protected int winRate;

        protected override async Task OnInitializedAsync()
        {
            var userId = Security.User?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            stats = await PermitIntelService.GetDashboardStats(userId);
            currentSubscription = await PermitIntelService.GetActiveSubscription(userId);

            // Build chart data from borough stats
            if (stats.PermitsByBorough?.Any() == true)
            {
                boroughData = stats.PermitsByBorough
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => new BoroughChartItem { Borough = kvp.Key, Count = kvp.Value })
                    .ToList();
            }

            // Calculate win rate
            winRate = stats.SavedLeadsCount > 0
                ? (int)Math.Round((double)stats.WonLeadsCount / stats.SavedLeadsCount * 100)
                : 0;
        }

        public class BoroughChartItem
        {
            public string Borough { get; set; }
            public int Count { get; set; }
        }
    }
}
