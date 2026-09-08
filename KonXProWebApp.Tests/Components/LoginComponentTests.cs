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

public class LoginComponentTests : TestContext
{
    public LoginComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddRadzenComponents();
        Services.AddSingleton(new Mock<IHttpClientFactory>().Object);
        Services.AddSingleton(sp =>
            new SecurityService(sp.GetRequiredService<NavigationManager>(), sp.GetRequiredService<IHttpClientFactory>()));
    }

    [Fact]
    public void LoginLayout_RendersAuthLayoutAndBrandLogoWithoutPhotographicBackgroundOrOrangeX()
    {
        var cut = RenderComponent<LoginLayout>();

        // Must not contain photographic background or orange X color code
        Assert.DoesNotContain("login.jpg", cut.Markup);
        Assert.DoesNotContain("#ffc107", cut.Markup);

        // Must contain auth layout, card, and official logo structure
        Assert.Contains("auth-layout", cut.Markup);
        Assert.Contains("auth-card", cut.Markup);
        Assert.Contains("logo-icon", cut.Markup);
        Assert.Contains("logo-text", cut.Markup);
        Assert.Contains("Kon<span>X</span>Pro", cut.Markup);

        // Must contain properly positioned footer text with copyright
        Assert.Contains("auth-footer", cut.Markup);
        Assert.Contains("KonXProWebApp v1.0.0, Copyright", cut.Markup);
    }

    [Fact]
    public void Login_RendersHeaderFormAndActionLinks()
    {
        var cut = RenderComponent<Login>();

        Assert.Contains("auth-header", cut.Markup);
        Assert.Contains("Sign In", cut.Markup);
        Assert.Contains("Enter your credentials to access your account", cut.Markup);
        Assert.Contains("Forgot password?", cut.Markup);
        Assert.Contains("Create an account", cut.Markup);
        Assert.Contains("account/login", cut.Markup);
    }

    [Fact]
    public void Login_HasAccessibleLabelsAndSubmitButton()
    {
        var cut = RenderComponent<Login>();

        // Explicit labels with for attribute matching input ids
        Assert.Contains("for=\"userName\"", cut.Markup);
        Assert.Contains("for=\"password\"", cut.Markup);
        Assert.Contains("id=\"userName\"", cut.Markup);
        Assert.Contains("id=\"password\"", cut.Markup);

        // Required ARIA attributes
        Assert.Contains("aria-required=\"true\"", cut.Markup);

        // Submit button has accessible name and type submit
        var submitBtn = cut.Find("button[type='submit']");
        Assert.NotNull(submitBtn);
        Assert.Equal("Log in to KonXPro", submitBtn.GetAttribute("aria-label"));
        Assert.Contains("Log In", submitBtn.TextContent);
    }
}
