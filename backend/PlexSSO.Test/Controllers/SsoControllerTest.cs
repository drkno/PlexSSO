using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PlexSSO.Controllers;
using PlexSSO.Model;
using PlexSSO.Model.API;
using PlexSSO.Service.Auth;

namespace PlexSSO.Test.Controllers;

public class SsoControllerTest
{
    [Test]
    public void Get_WhenAuthenticated_AddsBothHeaders()
    {
        var logger = Substitute.For<ILogger<SsoController>>();
        var authValidator = Substitute.For<IAuthValidator>();
        authValidator.ValidateAuthenticationStatus(default, default, default)
            .ReturnsForAnyArgs(new SsoResponse(true, true, false, PlexSSO.Model.Types.AccessTier.NormalUser, 200, ""));

        var controller = new SsoController(logger, authValidator);

        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new Claim(Constants.AccessTokenClaim, "token"),
            new Claim(Constants.UsernameClaim, "testuser"),
            new Claim(Constants.EmailClaim, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        httpContext.User = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        controller.Get();

        Assert.That(httpContext.Response.Headers.ContainsKey(Constants.SsoResponseUserHeader), Is.True);
        Assert.That(httpContext.Response.Headers.ContainsKey(Constants.SsoResponseEmailHeader), Is.True);
        Assert.That(httpContext.Response.Headers[Constants.SsoResponseUserHeader].ToString(), Is.EqualTo("testuser"));
        Assert.That(httpContext.Response.Headers[Constants.SsoResponseEmailHeader].ToString(), Is.EqualTo("test@example.com"));
    }
}
