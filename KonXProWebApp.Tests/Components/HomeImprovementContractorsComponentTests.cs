using Bunit;
using KonXProWebApp.Components.Pages;
using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen;
using System;
using System.Net.Http;
using Xunit;

namespace KonXProWebApp.Tests.Components
{
    public class HomeImprovementContractorsComponentTests : TestContext
    {
        private readonly db_9f8bee_konxdevContext _context;

        public HomeImprovementContractorsComponentTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;

            var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new db_9f8bee_konxdevContext(options);

            var identityOptions = new DbContextOptionsBuilder<ApplicationIdentityDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var identityContext = new ApplicationIdentityDbContext(identityOptions);

            Services.AddRadzenComponents();
            Services.AddSingleton(_context);
            Services.AddSingleton(identityContext);
            
            // Mock HttpClient
            var mockHttpClient = new Mock<HttpClient>();
            Services.AddSingleton(mockHttpClient.Object);

            // Mock IHttpClientFactory
            var mockHttpFactory = new Mock<IHttpClientFactory>();
            Services.AddSingleton(mockHttpFactory.Object);

            // Register SecurityService
            Services.AddSingleton(sp =>
                new SecurityService(sp.GetRequiredService<NavigationManager>(), mockHttpFactory.Object));
        }

        [Fact]
        public void Contractors_InitialLoad_RendersSeededContractor()
        {
            // Seed
            _context.HomeImprovementContractors.Add(new HomeImprovementContractor
            {
                LicenseNumber = "1234567-DCA",
                BusinessName = "ACME ROOFING CORP",
                DbaTradeName = "ACME ROOFING",
                LicenseStatus = "Active",
                ContactPhoneNumber = "555-0199",
                AddressCity = "Brooklyn",
                IngestedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            // Render component
            var cut = RenderComponent<HomeImprovementContractors>();

            // Assert business name is displayed
            var content = cut.Markup;
            Assert.Contains("ACME ROOFING CORP", content);
            Assert.Contains("1234567-DCA", content);
        }
    }
}
