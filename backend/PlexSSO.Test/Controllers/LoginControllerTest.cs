using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Test.Service.Auth;
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

        var controller = new LoginController(logger, plexClient, authValidator,
            TwoFactorPolicyFactory.Create(plexClient, requireTwoFactor: false));

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

    private static (LoginController, IPlexClient) CreateTwoFactorController(bool? twoFactorEnabled, params string[] exempt)
    {
        var plexClient = Substitute.For<IPlexClient>();
        plexClient.GetPlexServerIdentifier().Returns(new ServerIdentifier("test-server"));
        plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.NormalUser);
        plexClient.GetUserInfo(default!).ReturnsForAnyArgs(new User("testuser", "Test", "test@example.com", "", "uuid"));
        plexClient.IsTwoFactorEnabled(default!).ReturnsForAnyArgs(twoFactorEnabled);

        var authValidator = Substitute.For<IAuthValidator>();
        authValidator.ValidateAuthenticationStatus(default!, default!, default!)
            .ReturnsForAnyArgs(new SsoResponse(true, true, false, AccessTier.NormalUser, 200, ""));

        var controller = new LoginController(Substitute.For<ILogger<LoginController>>(), plexClient, authValidator,
            TwoFactorPolicyFactory.Create(plexClient, requireTwoFactor: true, exempt));
        var services = new ServiceCollection()
            .AddSingleton(Substitute.For<IAuthenticationService>())
            .BuildServiceProvider();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        };
        return (controller, plexClient);
    }

    [TestCase(false)]
    [TestCase(null)]
    public async Task Login_WhenTwoFactorRequiredButNotConfirmed_IsBlocked(bool? twoFactorEnabled)
    {
        var (controller, _) = CreateTwoFactorController(twoFactorEnabled);

        var result = await controller.Login(new LoginPost { Token = "token" });

        Assert.That(result.Status, Is.EqualTo(403));
        Assert.That(result.AccessBlocked, Is.True);
        Assert.That(result.LoggedIn, Is.False);
        Assert.That(controller.Response.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task Login_WhenTwoFactorRequiredAndEnabled_Succeeds()
    {
        var (controller, _) = CreateTwoFactorController(true);

        var result = await controller.Login(new LoginPost { Token = "token" });

        Assert.That(result.Status, Is.EqualTo(200));
    }

    [Test]
    public async Task Login_WhenTwoFactorRequiredButUserExempt_SkipsCheck()
    {
        var (controller, plexClient) = CreateTwoFactorController(false, "testuser");

        var result = await controller.Login(new LoginPost { Token = "token" });

        Assert.That(result.Status, Is.EqualTo(200));
        await plexClient.DidNotReceiveWithAnyArgs().IsTwoFactorEnabled(default!);
    }
}
