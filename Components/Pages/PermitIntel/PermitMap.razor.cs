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

        [Inject]
        protected SecurityService Security { get; set; }

        // Filters
        protected string selectedBorough;
        protected string selectedTrade;
        protected string selectedJobType;
        protected int? selectedMinScore;
        protected decimal? selectedMinCost;
        protected string searchText = "";
        protected bool isLoading = false;

        // KPI stats
        protected int mapPermitCount = 0;
        protected int hotLeadsCount = 0;
        protected string totalMapValue = "$0";

        public Exception LastException { get; private set; }
        public int MapPermitCount => mapPermitCount;
        public string SelectedBorough => selectedBorough;
        public string SelectedTrade => selectedTrade;

        private bool mapInitialized = false;
        private DotNetObjectReference<PermitMap> objRef;
        private List<DobjobFiling> currentFilings = new();

        protected List<string> boroughOptions = new() { "MANHATTAN", "BROOKLYN", "QUEENS", "BRONX", "STATEN ISLAND" };
        protected List<string> jobTypeOptions = new() { "A1", "A2", "A3", "NB", "DM", "SG" };

        protected List<KeyValuePair<string, string>> tradeOptions = new()
        {
            new("Plumbing", "Plumbing"),
            new("Mechanical", "Mechanical"),
            new("Boiler", "Boiler"),
            new("Sprinkler", "Sprinkler"),
            new("FireAlarm", "Fire Alarm"),
            new("Standpipe", "Standpipe"),
            new("Equipment", "Equipment"),
            new("FireSuppression", "Fire Suppression"),
            new("CurbCut", "Curb Cut")
        };

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

        protected override async Task OnInitializedAsync()
        {
            var userId = Security?.User?.Id;
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var preference = await PermitIntelService.GetAlertPreference(userId);
                    if (preference != null)
                    {
                        if (!string.IsNullOrWhiteSpace(preference.Boroughs))
                        {
                            var savedBoroughs = preference.Boroughs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (savedBoroughs.Length > 0)
                            {
                                var matchingBoro = boroughOptions.FirstOrDefault(b => string.Equals(b, savedBoroughs[0], StringComparison.OrdinalIgnoreCase));
                                if (matchingBoro != null)
                                {
                                    selectedBorough = matchingBoro;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(preference.Trades))
                        {
                            var savedTrades = preference.Trades.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (savedTrades.Length > 0)
                            {
                                var normalized = savedTrades[0].Replace(" ", "");
                                var matchingTrade = tradeOptions.FirstOrDefault(t => string.Equals(t.Key, normalized, StringComparison.OrdinalIgnoreCase));
                                if (!string.IsNullOrEmpty(matchingTrade.Key))
                                {
                                    selectedTrade = matchingTrade.Key;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(preference.JobTypes))
                        {
                            var savedJobTypes = preference.JobTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (savedJobTypes.Length > 0 && jobTypeOptions.Contains(savedJobTypes[0]))
                            {
                                selectedJobType = savedJobTypes[0];
                            }
                        }

                        if (preference.MinCost.HasValue)
                        {
                            selectedMinCost = preference.MinCost;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading saved criteria: {ex.Message}");
                }
            }

            // Pre-load data ahead of first render so KPI stats are available immediately
            await FetchFilingsAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                objRef = DotNetObjectReference.Create(this);

                // Initial position: center on user's saved borough preset if set, else ALL NYC
                double initLat = 40.7128;
                double initLng = -74.0060;
                int initZoom = 11;

                if (!string.IsNullOrEmpty(selectedBorough) && BoroughCenters.TryGetValue(selectedBorough, out var preset))
                {
                    initLat = preset.Lat;
                    initLng = preset.Lng;
                    initZoom = preset.Zoom;
                }

                try
                {
                    await JSRuntime.InvokeVoidAsync("leafletInterop.initializeMap", "permit-map", initLat, initLng, initZoom);
                    mapInitialized = true;
                    await RenderMarkersAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing map: {ex.Message}");
                }
            }
        }

        private async Task FetchFilingsAsync()
        {
            isLoading = true;
            try
            {
                var query = new PermitSearchQuery
                {
                    SearchText = searchText,
                    Boroughs = !string.IsNullOrEmpty(selectedBorough) ? new() { selectedBorough } : new(),
                    JobTypes = !string.IsNullOrEmpty(selectedJobType) ? new() { selectedJobType } : new(),
                    Trades = !string.IsNullOrEmpty(selectedTrade) ? new() { selectedTrade } : new(),
                    MinCost = selectedMinCost,
                    RequireGisCoordinates = true,
                    Take = 500
                };

                var (results, count) = await PermitIntelService.SearchPermits(query);

                var filtered = results;
                if (selectedMinScore.HasValue)
                {
                    filtered = filtered.Where(p => (p.LeadScore ?? 1) >= selectedMinScore.Value).ToList();
                }

                currentFilings = filtered.ToList();
                mapPermitCount = currentFilings.Count;
                hotLeadsCount = currentFilings.Count(p => (p.LeadScore ?? 1) >= 4);

                var totalCost = currentFilings.Sum(p => p.InitialCost ?? 0m);
                totalMapValue = totalCost switch
                {
                    >= 1_000_000m => $"${(totalCost / 1_000_000m):N1}M",
                    >= 1_000m => $"${(totalCost / 1_000m):N0}K",
                    _ => totalCost.ToString("C0")
                };
            }
            catch (Exception ex)
            {
                LastException = ex;
                Console.WriteLine($"FetchFilingsAsync error: {ex}");
                NotificationService?.Notify(NotificationSeverity.Error, "Map Error", ex.Message);
            }
            finally
            {
                isLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task RenderMarkersAsync()
        {
            if (!mapInitialized || JSRuntime == null) return;

            try
            {
                var markers = currentFilings
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
                Console.WriteLine($"Error rendering markers: {ex.Message}");
            }
        }

        protected async Task LoadMapData()
        {
            await FetchFilingsAsync();
            StateHasChanged();
            await RenderMarkersAsync();
        }

        protected async Task OnBoroughChanged(object value)
        {
            if (mapInitialized && !string.IsNullOrEmpty(selectedBorough) && BoroughCenters.TryGetValue(selectedBorough, out var preset))
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("leafletInterop.panToLocation", preset.Lat, preset.Lng, preset.Zoom);
                }
                catch { }
            }
            await LoadMapData();
        }

        protected async Task ResetFilters()
        {
            selectedBorough = null;
            selectedTrade = null;
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
