using KonXProWebApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KonXProWebApp.Tests.Controllers;

public class AccountControllerTests
{
    [Fact]
    public void RegisterGet_RedirectsToLoginWithRegisterParam()
    {
        var controller = new AccountController(null, null, null, null, null, null);

        var result = controller.RegisterGet(null, null) as RedirectResult;

        Assert.NotNull(result);
        Assert.Equal("~/Login?register=true", result.Url);
    }

    [Fact]
    public void RegisterGet_WithTierAndReturnUrl_PreservesQueryParams()
    {
        var controller = new AccountController(null, null, null, null, null, null);

        var result = controller.RegisterGet("/pricing", "Pro") as RedirectResult;

        Assert.NotNull(result);
        Assert.Contains("register=true", result.Url);
        Assert.Contains("tier=Pro", result.Url);
        Assert.Contains("redirectUrl=", result.Url);
    }
}
