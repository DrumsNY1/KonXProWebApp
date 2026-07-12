using Radzen;
using KonXProWebApp.Components;
using Microsoft.EntityFrameworkCore;
using KonXProWebApp.Data;
using Microsoft.AspNetCore.Identity;
using KonXProWebApp.Models;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddHubOptions(options => options.MaximumReceiveMessageSize = 10 * 1024 * 1024);
builder.Services.AddControllers();
builder.Services.AddRadzenComponents();
builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "KonXProWebAppTheme";
    options.Duration = TimeSpan.FromDays(365);
});
builder.Services.AddHttpClient();
builder.Services.AddScoped<KonXProWebApp.db_9f8bee_konxdevService>();
builder.Services.AddScoped<KonXProWebApp.Services.PermitIntelService>();
builder.Services.AddScoped<KonXProWebApp.Services.StripeService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, KonXProWebApp.Authorization.SubscriptionAuthorizationHandler>();
builder.Services.AddDbContext<KonXProWebApp.Data.db_9f8bee_konxdevContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("db_9f8bee_konxdevConnection"));
});
builder.Services.AddHttpClient("KonXProWebApp").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false }).AddHeaderPropagation(o => o.Headers.Add("Cookie"));
builder.Services.AddHeaderPropagation(o => o.Headers.Add("Cookie"));
builder.Services.AddAuthentication();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequiresStarter", policy => policy.Requirements.Add(new KonXProWebApp.Authorization.SubscriptionRequirement("Starter")));
    options.AddPolicy("RequiresPro", policy => policy.Requirements.Add(new KonXProWebApp.Authorization.SubscriptionRequirement("Pro")));
    options.AddPolicy("RequiresBusiness", policy => policy.Requirements.Add(new KonXProWebApp.Authorization.SubscriptionRequirement("Business")));
    options.AddPolicy("RequiresAgency", policy => policy.Requirements.Add(new KonXProWebApp.Authorization.SubscriptionRequirement("Agency")));
});
builder.Services.AddScoped<KonXProWebApp.SecurityService>();
builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("db_9f8bee_konxdevConnection"));
});
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<ApplicationIdentityDbContext>().AddDefaultTokenProviders();
builder.Services.AddTransient<IUserStore<ApplicationUser>, MultiTenancyUserStore>();
builder.Services.AddControllers().AddOData(o =>
{
    var oDataBuilder = new ODataConventionModelBuilder();
    oDataBuilder.EntitySet<ApplicationUser>("ApplicationUsers");
    var usersType = oDataBuilder.StructuralTypes.First(x => x.ClrType == typeof(ApplicationUser));
    usersType.AddProperty(typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Password)));
    usersType.AddProperty(typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.ConfirmPassword)));
    oDataBuilder.EntitySet<ApplicationRole>("ApplicationRoles");
    oDataBuilder.EntitySet<ApplicationTenant>("ApplicationTenants");
    o.AddRouteComponents("odata/Identity", oDataBuilder.GetEdmModel()).Count().Filter().OrderBy().Expand().Select().SetMaxTop(null).TimeZone = TimeZoneInfo.Utc;
});
builder.Services.AddScoped<AuthenticationStateProvider, KonXProWebApp.ApplicationAuthenticationStateProvider>();
var app = builder.Build();
var forwardingOptions = new ForwardedHeadersOptions()
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardingOptions.KnownNetworks.Clear();
forwardingOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardingOptions);
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapControllers();
app.UseHeaderPropagation();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>().Database.Migrate();
app.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>().SeedTenantsAdmin().Wait();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<KonXProWebApp.Data.db_9f8bee_konxdevContext>();
    var views = new[]
    {
        @"CREATE OR ALTER VIEW dbo.vwFreeTierDashboard AS
          SELECT JobNum, Borough, ISNULL(HouseNum, '') + ' ' + ISNULL(StreetName, '') AS Street, LatestActionDate, JobType AS ProjectType, JobDescription, Gisntaname AS Neighborhood
          FROM dbo.DOBJobFilings;",
          
        @"CREATE OR ALTER VIEW dbo.vwBasicTierDashboard AS
          SELECT JobNum, Borough, HouseNum, StreetName AS Street, LatestActionDate, JobType AS ProjectType, JobDescription, Gisntaname AS Neighborhood
          FROM dbo.DOBJobFilings;",
          
        @"CREATE OR ALTER VIEW dbo.vwMidTierDashboard AS
          SELECT JobNum, Borough, HouseNum, StreetName AS Street, LatestActionDate, JobType AS ProjectType, InitialCost AS EstimatedCost, JobDescription, Gisntaname AS Neighborhood
          FROM dbo.DOBJobFilings;",
          
        @"CREATE OR ALTER VIEW dbo.vwHighTierDashboard AS
          SELECT JobNum, Borough, HouseNum, StreetName AS Street, LatestActionDate, JobType AS ProjectType, InitialCost AS EstimatedCost, JobDescription, Gisntaname AS Neighborhood
          FROM dbo.DOBJobFilings;",
          
        @"CREATE OR ALTER VIEW dbo.vwDemoDisplay AS
          SELECT 'Sample Content' AS Content, 'Sample Summary' AS Summary, CAST(GETDATE() AS datetime2) AS CompletionDate;"
    };

    foreach (var viewSql in views)
    {
        context.Database.ExecuteSqlRaw(viewSql);
    }
}

app.Run();