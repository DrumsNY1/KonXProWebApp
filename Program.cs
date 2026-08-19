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
builder.Services.AddScoped<KonXProWebApp.Services.ContractorIntelService>();
builder.Services.AddScoped<KonXProWebApp.Services.StripeService>();
builder.Services.AddScoped<KonXProWebApp.Services.SubscriptionTierService>();
builder.Services.AddScoped<KonXProWebApp.Services.ComplianceIntelService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, KonXProWebApp.Authorization.SubscriptionAuthorizationHandler>();
builder.Services.AddDbContext<KonXProWebApp.Data.db_9f8bee_konxdevContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("db_9f8bee_konxdevConnection"));
});
builder.Services.AddHttpClient("KonXProWebApp", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false }).AddHeaderPropagation(o => o.Headers.Add("Cookie"));
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

app.UseHttpMethodOverride();
app.UseHttpsRedirection();
app.MapControllers();
app.UseHeaderPropagation();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Attempting database migration and tenant seeding...");
        var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();
        identityDb.Database.Migrate();
        identityDb.SeedTenantsAdmin().Wait();

        var permitDb = scope.ServiceProvider.GetRequiredService<KonXProWebApp.Data.db_9f8bee_konxdevContext>();
        logger.LogInformation("Ensuring permit intel database schema is created...");
        permitDb.Database.EnsureCreated();

        var tableDdl = @"
            IF OBJECT_ID('dbo.Subscriptions', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Subscriptions (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId NVARCHAR(450) NOT NULL,
                    StripeCustomerId NVARCHAR(255) NULL,
                    StripeSubscriptionId NVARCHAR(255) NULL,
                    Tier NVARCHAR(50) NOT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    StartDate DATETIME2 NOT NULL,
                    EndDate DATETIME2 NULL,
                    TrialEndDate DATETIME2 NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
                CREATE INDEX IX_Subscriptions_UserId ON dbo.Subscriptions(UserId);
            END;
            IF OBJECT_ID('dbo.SavedLeads', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.SavedLeads (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId NVARCHAR(450) NOT NULL,
                    DobjobFilingId INT NOT NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'New',
                    Notes NVARCHAR(MAX) NULL,
                    EstimatedValue DECIMAL(18,2) NULL,
                    Tags NVARCHAR(500) NULL,
                    SavedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
                CREATE INDEX IX_SavedLeads_UserId ON dbo.SavedLeads(UserId);
            END;
            IF OBJECT_ID('dbo.AlertPreferences', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AlertPreferences (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    UserId NVARCHAR(450) NOT NULL,
                    Boroughs NVARCHAR(500) NULL,
                    JobTypes NVARCHAR(500) NULL,
                    Trades NVARCHAR(500) NULL,
                    MinCost DECIMAL(18,2) NULL,
                    MaxCost DECIMAL(18,2) NULL,
                    ZipCodes NVARCHAR(500) NULL,
                    EmailEnabled BIT NOT NULL DEFAULT 1,
                    SmsEnabled BIT NOT NULL DEFAULT 0,
                    PushEnabled BIT NOT NULL DEFAULT 1,
                    Frequency NVARCHAR(50) NOT NULL DEFAULT 'Instant',
                    PhoneNumber NVARCHAR(50) NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
                CREATE INDEX IX_AlertPreferences_UserId ON dbo.AlertPreferences(UserId);
            END;";

        try { permitDb.Database.ExecuteSqlRaw(tableDdl); } catch (Exception exTbl) { logger.LogWarning(exTbl, "Table creation warning"); }

        identityDb.SeedTierTestUsersAsync(permitDb).Wait();

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
            try
            {
                permitDb.Database.ExecuteSqlRaw(viewSql);
            }
            catch (Exception exView)
            {
                logger.LogWarning(exView, "View creation skipped or non-fatal error");
            }
        }
    }
    catch (Exception ex)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An exception occurred while migrating or seeding the database on startup.");
    }
}

app.Run();

/// <summary>
/// Exposes the top-level Program as a public partial class so integration tests can boot the app
/// in-process via WebApplicationFactory&lt;Program&gt;.
/// </summary>
public partial class Program { }