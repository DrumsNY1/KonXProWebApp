using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace KonXProWebApp.Components.Layout
{
    public partial class MainLayout : IDisposable
    {
        private const int MobileBreakpoint = 768;

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
        protected SecurityService Security { get; set; }

        // Start closed — OnAfterRenderAsync will open it on desktop
        protected bool isSidebarOpen = false;
        private bool _isMobileViewport = true;

        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await DetectViewportAndSetSidebar();
            }
        }

        private async Task DetectViewportAndSetSidebar()
        {
            try
            {
                var width = await JSRuntime.InvokeAsync<int>("eval", "window.innerWidth");
                _isMobileViewport = width <= MobileBreakpoint;
                isSidebarOpen = !_isMobileViewport; // Open on desktop, closed on mobile
                StateHasChanged();
            }
            catch
            {
                // During prerender or if JS interop is unavailable, keep closed
                isSidebarOpen = false;
            }
        }

        private void NavigateToLogin()
        {
            NavigationManager.NavigateTo("login");
        }

        protected void ProfileMenuClick(RadzenProfileMenuItem args)
        {
            if (args.Value == "Logout")
            {
                Security.Logout();
            }
        }

        protected void ToggleSidebar()
        {
            isSidebarOpen = !isSidebarOpen;
        }

        protected void CloseSidebar()
        {
            if (_isMobileViewport)
            {
                isSidebarOpen = false;
            }
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            // Auto-close sidebar on navigation at mobile widths
            if (_isMobileViewport && isSidebarOpen)
            {
                isSidebarOpen = false;
                InvokeAsync(StateHasChanged);
            }
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}

