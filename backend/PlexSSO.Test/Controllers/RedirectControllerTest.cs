using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PlexSSO.Controllers;
using PlexSSO.Service;
using PlexSSO.Service.Config;

namespace PlexSSO.Test.Controllers;

public class RedirectControllerTest
{
    private static RedirectController CreateController()
    {
        var controller = new RedirectController(
            Substitute.For<IConfigurationService>(),
            new List<ITokenService>(),
            Substitute.For<ILogger<RedirectController>>());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("login.example.com");
        httpContext.Request.Scheme = "https";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [TestCase("sonarr")]
    [TestCase("your-2021")]
    [TestCase("a")]
    public void IsValidSubdomain_AcceptsDnsLabels(string subdomain)
    {
        Assert.That(RedirectController.IsValidSubdomain(subdomain), Is.True);
    }

    [TestCase("evil.com#")]
    [TestCase("evil.com?")]
    [TestCase("evil.com")]
    [TestCase("user@evil")]
    [TestCase("-sonarr")]
    [TestCase("")]
    [TestCase(null)]
    public void IsValidSubdomain_RejectsAnythingElse(string? subdomain)
    {
        Assert.That(RedirectController.IsValidSubdomain(subdomain!), Is.False);
    }

    [Test]
    public async Task Get_WithHostEscapingSubdomain_DoesNotRedirect()
    {
        var controller = CreateController();

        var result = await controller.Get("evil.com#");

        Assert.That(result.Success, Is.False);
        Assert.That(controller.Response.StatusCode, Is.EqualTo(400));
        Assert.That(controller.Response.Headers.ContainsKey("Location"), Is.False);
    }

    [Test]
    public async Task Get_WithValidSubdomain_RedirectsWithinDomain()
    {
        var controller = CreateController();

        await controller.Get("sonarr/calendar");

        Assert.That(controller.Response.StatusCode, Is.EqualTo(302));
        Assert.That(controller.Response.Headers["Location"].ToString(),
            Is.EqualTo("https://sonarr.example.com/calendar"));
    }
}
