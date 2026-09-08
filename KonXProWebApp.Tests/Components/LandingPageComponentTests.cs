using Bunit;
using KonXProWebApp.Components.Layout;
using KonXProWebApp.Components.Pages;
using KonXProWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen;
using Xunit;

namespace KonXProWebApp.Tests.Components;

public class LandingPageComponentTests : TestContext
{
    public LandingPageComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddRadzenComponents();
        Services.AddSingleton(new Mock<IHttpClientFactory>().Object);
        Services.AddSingleton(sp =>
            new SecurityService(sp.GetRequiredService<NavigationManager>(), sp.GetRequiredService<IHttpClientFactory>()));
    }

    [Fact]
    public void LandingLayout_RendersValidNavLinksAndActions()
    {
        var cut = RenderComponent<LandingLayout>();

        // Navigation anchors
        Assert.Contains("href=\"/#how\"", cut.Markup);
        Assert.Contains("href=\"/#features\"", cut.Markup);
        Assert.Contains("href=\"/#pricing\"", cut.Markup);

        // Action buttons
        Assert.Contains("Sign In", cut.Markup);
        Assert.Contains("Start Free Trial", cut.Markup);

        // Logo
        Assert.Contains("href=\"/\"", cut.Markup);
        Assert.Contains("Kon<span>X</span>Pro", cut.Markup);
    }

    [Fact]
    public void Index_DoesNotContainDeadHrefHashLinksInFooter()
    {
        var cut = RenderComponent<KonXProWebApp.Components.Pages.Index>();

        // Footer must not contain dead href="#" links
        var footer = cut.Find("footer");
        Assert.DoesNotContain("href=\"#\"", footer.OuterHtml);

        // Footer must link to active feature anchors and pages
        Assert.Contains("href=\"#feature-feed\"", footer.OuterHtml);
        Assert.Contains("href=\"#feature-map\"", footer.OuterHtml);
        Assert.Contains("href=\"#feature-alerts\"", footer.OuterHtml);
        Assert.Contains("href=\"#feature-crm\"", footer.OuterHtml);
        Assert.Contains("href=\"#feature-reports\"", footer.OuterHtml);

        Assert.Contains("href=\"/about-us\"", footer.OuterHtml);
        Assert.Contains("href=\"/contact-us\"", footer.OuterHtml);
        Assert.Contains("href=\"/privacy-policy\"", footer.OuterHtml);
        Assert.Contains("href=\"/terms-of-use\"", footer.OuterHtml);
        Assert.Contains("href=\"/api/health\"", footer.OuterHtml);
    }

    [Fact]
    public void Index_DisplaysDynamicCurrentYearInCopyright()
    {
        var cut = RenderComponent<KonXProWebApp.Components.Pages.Index>();
        var currentYear = DateTime.UtcNow.Year.ToString();

        var footerBottom = cut.Find(".footer-bottom");
        Assert.Contains($"© {currentYear} KonXPro. All rights reserved.", footerBottom.InnerHtml);
        Assert.DoesNotContain("© 2024", footerBottom.InnerHtml);
    }

    [Fact]
    public void Index_StatsBar_UsesVerifiableNYCDOBMetricsWithoutUnsubstantiatedClaims()
    {
        var cut = RenderComponent<KonXProWebApp.Components.Pages.Index>();

        // Must not contain unverified contractor counts or valuation claims
        Assert.DoesNotContain("8,500+", cut.Markup);
        Assert.DoesNotContain("$2.1B", cut.Markup);

        // Must contain verified DOB data metrics
        Assert.Contains("50,000", cut.Markup);
        Assert.Contains("Permits tracked annually", cut.Markup);
        Assert.Contains("NYC Boroughs covered daily", cut.Markup);
        Assert.Contains("Alert turnaround speed", cut.Markup);
        Assert.Contains("Public DOB records monitored", cut.Markup);
    }

    [Fact]
    public void Index_Testimonials_RefactoredToTradeWorkflows()
    {
        var cut = RenderComponent<KonXProWebApp.Components.Pages.Index>();

        // Must not contain unverified named contractors or unsubstantiated dollar/percentage claims
        Assert.DoesNotContain("Mike Rosario", cut.Markup);
        Assert.DoesNotContain("$94K plumbing job", cut.Markup);
        Assert.DoesNotContain("Denise Walton", cut.Markup);
        Assert.DoesNotContain("Tony Castellano", cut.Markup);
        Assert.DoesNotContain("closing rate has gone up at least 30%", cut.Markup);

        // Must contain Trade Workflows section and personas
        Assert.Contains("Trade Workflows", cut.Markup);
        Assert.Contains("Built for Every Trade Across NYC", cut.Markup);
        Assert.Contains("Plumbing & Mechanical", cut.Markup);
        Assert.Contains("General Contractors", cut.Markup);
        Assert.Contains("Electrical & Roofing", cut.Markup);
    }

    [Fact]
    public void PrivacyPolicy_RendersLegalContentWithoutLoremIpsum()
    {
        var cut = RenderComponent<PrivacyPolicy>();

        Assert.DoesNotContain("Lorem ipsum", cut.Markup);
        Assert.Contains("NYC Department of Buildings (DOB) Public Records Notice", cut.Markup);
        Assert.Contains("privacy@konxpro.com", cut.Markup);
    }

    [Fact]
    public void TermsOfUse_Renders16SectionTermsWithoutLoremIpsum()
    {
        var cut = RenderComponent<TermsOfUse>();

        Assert.DoesNotContain("Lorem ipsum", cut.Markup);
        Assert.Contains("NYC Department of Buildings (DOB) Public Records Disclaimer", cut.Markup);
        Assert.Contains("State of New York", cut.Markup);
        Assert.Contains("Mandatory Binding Arbitration", cut.Markup);
        Assert.Contains("legal@konxpro.com", cut.Markup);
    }

    [Fact]
    public void ContactUs_RendersAuthenticContactOptionsWithoutMetropolis()
    {
        var cut = RenderComponent<ContactUs>();

        Assert.DoesNotContain("Metropolis", cut.Markup);
        Assert.DoesNotContain("+1 (555) 123-4567", cut.Markup);
        Assert.Contains("sales@konxpro.com", cut.Markup);
        Assert.Contains("support@konxpro.com", cut.Markup);
        Assert.Contains("Send Us a Message", cut.Markup);
    }

    [Fact]
    public void AboutUs_RendersKonXProMissionWithoutGenericRealEstate()
    {
        var cut = RenderComponent<AboutUs>();

        Assert.DoesNotContain("Senior Real Estate Advisor", cut.Markup);
        Assert.DoesNotContain("Metropolis", cut.Markup);
        Assert.Contains("Built for NYC Trade Contractors", cut.Markup);
        Assert.Contains("Why Timing Is Everything in NYC", cut.Markup);
    }
}
