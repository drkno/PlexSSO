using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PlexSSO.Controllers;
using PlexSSO.Model;
using PlexSSO.Model.API;
using PlexSSO.Model.Types;
using PlexSSO.Service.Auth;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Test.Controllers;

public class LoginControllerTest
{
    [Test]
    public async Task Login_WhenAlreadyAuthenticated_FetchesNewAccessTier()
    {
        var logger = Substitute.For<ILogger<LoginController>>();
        var plexClient = Substitute.For<IPlexClient>();
        var authValidator = Substitute.For<IAuthValidator>();

        plexClient.GetPlexServerIdentifier().Returns(new ServerIdentifier("test-server"));
        
        // Mock a failure to fetch AccessTier so it throws/returns Failure, which should fail the login.
        // If the code skipped fetching AccessTier because the user is already authenticated, this would not trigger.
        plexClient.GetAccessTier(Arg.Any<ServerIdentifier>(), Arg.Any<AccessToken>())
            .Returns(Task.FromResult(AccessTier.Failure));

        var controller = new LoginController(logger, plexClient, authValidator);

        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new Claim(Constants.AccessTokenClaim, "old-token"),
            new Claim(Constants.UsernameClaim, "testuser"),
            new Claim(Constants.EmailClaim, "test@example.com"),
            new Claim(Constants.AccessTierClaim, "Owner")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        httpContext.User = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var data = new LoginPost { Token = "new-token" };
        var result = await controller.Login(data);

        // Assert that GetAccessTier was called with the NEW token
        await plexClient.Received(1).GetAccessTier(Arg.Any<ServerIdentifier>(), Arg.Is<AccessToken>(t => t.Value == "new-token"));
        
        // Assert that the result is an error because AccessTier was Failure
        Assert.That(result.Success, Is.False);
        Assert.That(result.Status, Is.EqualTo(400));
    }
}
