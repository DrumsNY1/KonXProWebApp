using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;

namespace KonXProWebApp.Components.Pages.PermitIntel
{
    public partial class PermitMap : IAsyncDisposable
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

        protected string selectedBorough;
        protected string selectedJobType;
        protected int mapPermitCount = 0;
        private bool mapInitialized = false;

        protected List<string> boroughOptions = new() { "MANHATTAN", "BROOKLYN", "QUEENS", "BRONX", "STATEN ISLAND" };
        protected List<string> jobTypeOptions = new() { "A1", "A2", "A3", "NB", "DM", "SG" };

        private DotNetObjectReference<PermitMap> objRef;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                objRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("leafletInterop.initializeMap", "permit-map", 40.7128, -74.0060, 11);
                mapInitialized = true;
                await LoadMapData();
            }
        }

        protected async Task LoadMapData()
        {
            if (!mapInitialized) return;

            var query = new PermitSearchQuery
            {
                Boroughs = !string.IsNullOrEmpty(selectedBorough) ? new() { selectedBorough } : new(),
                JobTypes = !string.IsNullOrEmpty(selectedJobType) ? new() { selectedJobType } : new(),
                DateFrom = DateTime.Today.AddDays(-30),
                Take = 500  // Limit map pins for performance
            };

            var (results, count) = await PermitIntelService.SearchPermits(query);
            mapPermitCount = count;

            // Build markers JSON for JS
            var markers = results
                .Where(p => !string.IsNullOrEmpty(p.Gislatitude) && !string.IsNullOrEmpty(p.Gislongitude))
                .Select(p => new
                {
                    id = p.Id,
                    lat = double.TryParse(p.Gislatitude, out var lat) ? lat : 0,
                    lng = double.TryParse(p.Gislongitude, out var lng) ? lng : 0,
                    title = $"{p.HouseNum} {p.StreetName}",
                    borough = p.Borough,
                    jobType = p.JobType,
                    status = p.JobStatus,
                    cost = p.InitialCost?.ToString("C0") ?? "N/A",
                    score = p.LeadScore ?? 1
                })
                .Where(m => m.lat != 0 && m.lng != 0)
                .ToList();

            await JSRuntime.InvokeVoidAsync("leafletInterop.clearMarkers");
            await JSRuntime.InvokeVoidAsync("leafletInterop.addMarkers", System.Text.Json.JsonSerializer.Serialize(markers), objRef);

            StateHasChanged();
        }

        [JSInvokable]
        public void OnMarkerClicked(int permitId)
        {
            NavigationManager.NavigateTo($"/permit-intel/detail/{permitId}");
        }

        public async ValueTask DisposeAsync()
        {
            if (mapInitialized)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("leafletInterop.destroyMap");
                }
                catch { }
            }
            objRef?.Dispose();
        }
    }
}
