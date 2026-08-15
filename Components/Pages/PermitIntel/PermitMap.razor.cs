using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using System.Text.Json;

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
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public KonXProWebApp.Services.PermitIntelService PermitIntelService { get; set; }

        // Filters
        protected string selectedBorough;
        protected string selectedJobType;
        protected int? selectedMinScore;
        protected decimal? selectedMinCost;
        protected string searchText = "";
        protected bool isLoading = false;

        // KPI stats
        protected int mapPermitCount = 0;
        protected int hotLeadsCount = 0;
        protected string totalMapValue = "$0";

        private bool mapInitialized = false;
        private DotNetObjectReference<PermitMap> objRef;

        protected List<string> boroughOptions = new() { "MANHATTAN", "BROOKLYN", "QUEENS", "BRONX", "STATEN ISLAND" };
        protected List<string> jobTypeOptions = new() { "A1", "A2", "A3", "NB", "DM", "SG" };

        protected List<KeyValuePair<int?, string>> scoreOptions = new()
        {
            new(4, "🔥 4+ Stars (Hot)"),
            new(3, "⚡ 3+ Stars (High Priority)"),
            new(2, "📋 2+ Stars")
        };

        protected List<KeyValuePair<decimal?, string>> costOptions = new()
        {
            new(10_000m, "$10,000+"),
            new(50_000m, "$50,000+"),
            new(100_000m, "$100,000+"),
            new(500_000m, "$500,000+")
        };

        private static readonly Dictionary<string, (double Lat, double Lng, int Zoom)> BoroughCenters = new()
        {
            ["MANHATTAN"] = (40.7831, -73.9712, 12),
            ["BROOKLYN"] = (40.6782, -73.9442, 12),
            ["QUEENS"] = (40.7282, -73.7949, 11),
            ["BRONX"] = (40.8448, -73.8648, 12),
            ["STATEN ISLAND"] = (40.5795, -74.1502, 11)
        };

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

            isLoading = true;
            StateHasChanged();

            try
            {
                var query = new PermitSearchQuery
                {
                    SearchText = searchText,
                    Boroughs = !string.IsNullOrEmpty(selectedBorough) ? new() { selectedBorough } : new(),
                    JobTypes = !string.IsNullOrEmpty(selectedJobType) ? new() { selectedJobType } : new(),
                    MinCost = selectedMinCost,
                    Take = 500
                };

                var (results, count) = await PermitIntelService.SearchPermits(query);

                // Filter by min lead score client side if specified
                var filtered = results;
                if (selectedMinScore.HasValue)
                {
                    filtered = filtered.Where(p => (p.LeadScore ?? 1) >= selectedMinScore.Value).ToList();
                }

                mapPermitCount = filtered.Count();
                hotLeadsCount = filtered.Count(p => (p.LeadScore ?? 1) >= 4);

                var totalCost = filtered.Sum(p => p.InitialCost ?? 0m);
                totalMapValue = totalCost switch
                {
                    >= 1_000_000m => $"${(totalCost / 1_000_000m):N1}M",
                    >= 1_000m => $"${(totalCost / 1_000m):N0}K",
                    _ => totalCost.ToString("C0")
                };

                var markers = filtered
                    .Where(p => !string.IsNullOrEmpty(p.Gislatitude) && !string.IsNullOrEmpty(p.Gislongitude))
                    .Select(p => new
                    {
                        id = p.Id,
                        lat = double.TryParse(p.Gislatitude, out var lat) ? lat : 0,
                        lng = double.TryParse(p.Gislongitude, out var lng) ? lng : 0,
                        title = $"{p.HouseNum} {p.StreetName}",
                        borough = p.Borough,
                        bin = p.Bin,
                        jobType = p.JobType,
                        status = p.JobStatus,
                        cost = p.InitialCost?.ToString("C0") ?? "N/A",
                        score = p.LeadScore ?? 1,
                        factors = p.LeadScoreBreakdown?.Factors?.Select(f => new
                        {
                            name = f.Name,
                            points = f.Points,
                            style = f.BadgeStyle
                        }).ToList()
                    })
                    .Where(m => m.lat != 0 && m.lng != 0)
                    .ToList();

                await JSRuntime.InvokeVoidAsync("leafletInterop.clearMarkers");
                await JSRuntime.InvokeVoidAsync("leafletInterop.addMarkers", JsonSerializer.Serialize(markers), objRef);
            }
            catch (Exception ex)
            {
                NotificationService?.Notify(NotificationSeverity.Error, "Map Error", ex.Message);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        protected async Task OnBoroughChanged(object value)
        {
            if (!string.IsNullOrEmpty(selectedBorough) && BoroughCenters.TryGetValue(selectedBorough, out var preset))
            {
                await JSRuntime.InvokeVoidAsync("leafletInterop.panToLocation", preset.Lat, preset.Lng, preset.Zoom);
            }
            await LoadMapData();
        }

        protected async Task ResetFilters()
        {
            selectedBorough = null;
            selectedJobType = null;
            selectedMinScore = null;
            selectedMinCost = null;
            searchText = "";
            await LoadMapData();
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
