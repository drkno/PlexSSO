using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PlexSSO.Model;
using PlexSSO.Model.API;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using PlexSSO.Service.Auth;
using PlexSSO.Test.Service.Config;

namespace PlexSSO.Test.Service.Auth;

public class AuthenticationValidatorTest
{
    private ILogger<AuthenticationValidator>? _mockLogger;
    private PlexSsoConfig? _config;

    [SetUp]
    public void Setup()
    {
        _mockLogger = Substitute.For<ILogger<AuthenticationValidator>>();

        _config = new PlexSsoConfig()
        {
            ConfigFile = "/config/",
            PluginDirectory = "/app",
            PlexPreferencesFile = null,
            ServerIdentifier = new ServerIdentifier("xxxxxxxx"),
            CookieDomain = ".domain.com",
            DefaultAccessDeniedMessage =
                "Sorry, you were denied access to this page. Contact admin if you think this was an error.",
            AccessControls = new Dictionary<string, PlexSsoConfig.AccessControl[]>()
            {
                {
                    "status", new[]
                    {
                        new PlexSsoConfig.AccessControl()
                        {
                            Path = "/",
                            MinimumAccessTier = AccessTier.Owner,
                            ControlType = PlexSsoConfig.ControlType.Block,
                            BlockMessage = "You do not have permission to access this page.",
                        }
                    }
                }
            }
        };
    }

    [Test]
    public void Validate_NotAuthenticated_ReturnsDefaultAccessDeniedMessage()
    {
        var validator = CreateValidator();
        var identity = new Identity(new List<Claim>()) { IsAuthenticated = false };
        var serviceName = new ServiceName(_config!.AccessControls.Keys.First());
        var serviceUri = new ServiceUri("/");

        var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);

        AssertSsoResponse(result, new SsoResponse(
            success: true,
            loggedIn: false,
            blocked: true,
            accessTier: AccessTier.NoAccess,
            status: 401,
            message: Constants.DefaultLoginRequiredMessage
        ));
    }

    [Test]
    public void Validate_WithNoAccess_CustomControl_ReturnsCustomisedAccessDeniedMessage()
    {
        var validator = CreateValidator();
        var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, AccessTier = AccessTier.NormalUser };
        var serviceName = new ServiceName(_config!.AccessControls.Keys.First());
        var serviceUri = new ServiceUri("/");

        _config.AccessControls[serviceName.Value].First().BlockMessage = "Test Access Denied Message";

        var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);

        AssertSsoResponse(result, new SsoResponse(
            success: true,
            loggedIn: true,
            blocked: true,
            accessTier: AccessTier.NoAccess,
            status: 403,
            message: _config.AccessControls.Values.First().First().BlockMessage
        ));
    }
    
    [Test]
    public void Validate_WithExemptAccess_CustomControl_IsAllowed()
    {
        var validator = CreateValidator();
        var username = new Username("testuser", "testuser@testplace.com");
        var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, Username = username, AccessTier = AccessTier.NormalUser};
        var serviceName = new ServiceName(_config!.AccessControls.Keys.First());
        var serviceUri = new ServiceUri("/");

        _config.AccessControls[serviceName.Value].First().BlockMessage = "Test Access Denied Message";
        _config.AccessControls[serviceName.Value].First().Exempt = [username];

        var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);

        AssertSsoResponse(result, new SsoResponse(
            success: true,
            loggedIn: true,
            blocked: false,
            accessTier: AccessTier.NormalUser,
            status: 200,
            message: Constants.DefaultAuthorizedMessage
        ));
    }

    [Test]
    public void Validate_WithNoAccess_DefaultControl_ReturnsCustomisedAccessDeniedMessage()
    {
        var validator = CreateValidator();
        var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, AccessTier = AccessTier.NoAccess };
        var serviceName = new ServiceName(""); // empty string to trigger default control
        var serviceUri = new ServiceUri(""); // empty string to trigger default control

        var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);

        AssertSsoResponse(result, new SsoResponse(
            success: true,
            loggedIn: true,
            blocked: true,
            accessTier: AccessTier.NoAccess,
            status: 403,
            message: _config.DefaultAccessDeniedMessage
        ));
    }

    [Test]
    public void Validate_WithFailure_DefaultControl_ReturnsDefaultAccessDeniedMessage()
    {
        var validator = CreateValidator();
        var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, AccessTier = AccessTier.Failure };
        var serviceName = new ServiceName(""); // empty string to trigger default control
        var serviceUri = new ServiceUri(""); // empty string to trigger default control

        var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);

        AssertSsoResponse(result, new SsoResponse(
            success: true,
            loggedIn: false,
            blocked: true,
            accessTier: AccessTier.NoAccess,
            status: 401,
            message: Constants.DefaultLoginRequiredMessage
        ));
    }


    [Test]
    [TestCase(AccessTier.NormalUser)]
    [TestCase(AccessTier.HomeUser)]
    [TestCase(AccessTier.Owner)]
    public void Validate_WithAccess_DefaultControl_IsAllowed(AccessTier accessTier)
    {
        var validator = CreateValidator();
        var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, AccessTier = accessTier };
        var serviceName = new ServiceName("");
        var serviceUri = new ServiceUri("");

        var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);

        AssertSsoResponse(result, new SsoResponse(
            success: true,
            loggedIn: true,
            blocked: false,
            accessTier: accessTier,
            status: 200,
            message: Constants.DefaultAuthorizedMessage
        ));
    }

    [Test]
    [TestCase(new AccessTier[0],
        new[] { AccessTier.NormalUser, AccessTier.HomeUser, AccessTier.Owner })]
    [TestCase(new[] { AccessTier.NormalUser },
        new[] { AccessTier.HomeUser, AccessTier.Owner })]
    [TestCase(new[] { AccessTier.NormalUser, AccessTier.HomeUser },
        new[] { AccessTier.Owner })]
    public void Validate_WithAccess_CustomBlockControl_IsAllowed(AccessTier[] blockedAccessTiers,
        AccessTier[] allowedAccessTiers)
    {
        // NoAccess is permanently blocked
        AccessTier[] allBlockedAccessTiers = [.. blockedAccessTiers, AccessTier.NoAccess];
        
        var validator = CreateValidator();
        var serviceName = new ServiceName("status");
        var serviceUri = new ServiceUri("/");

        _config.AccessControls = new Dictionary<string, PlexSsoConfig.AccessControl[]>()
        {
            {
                "status", new[]
                {
                    new PlexSsoConfig.AccessControl()
                    {
                        Path = "/",
                        MinimumAccessTier = allowedAccessTiers.First(),
                        ControlType = PlexSsoConfig.ControlType.Block,
                        BlockMessage = "You do not have permission to access this page.",
                    }
                }
            }
        };

        foreach (var blockedAccessTier in allBlockedAccessTiers)
        {
            var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, AccessTier = blockedAccessTier };
            var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);
            AssertSsoResponse(result, new SsoResponse(
                success: true,
                loggedIn: true,
                blocked: true,
                accessTier: AccessTier.NoAccess,
                status: 403,
                message: _config.AccessControls.Values.First().First().BlockMessage
            ));
        }

        foreach (var allowedAccessTier in allowedAccessTiers)
        {
            var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, AccessTier = allowedAccessTier };
            var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);
            AssertSsoResponse(result, new SsoResponse(
                success: true,
                loggedIn: true,
                blocked: false,
                accessTier: allowedAccessTier,
                status: 200,
                message: Constants.DefaultAuthorizedMessage
            ));
        }
    }

    [Test]
    [TestCase(new[] { AccessTier.NormalUser },
        new[] { AccessTier.HomeUser, AccessTier.Owner })]
    [TestCase(new[] { AccessTier.NormalUser, AccessTier.HomeUser },
        new[] { AccessTier.Owner })]
    [TestCase(new[] { AccessTier.NormalUser, AccessTier.HomeUser, AccessTier.Owner },
        new AccessTier[0])]
    public void Validate_WithAccess_CustomAllowControl_IsAllowed(AccessTier[] allowedAccessTiers,
        AccessTier[] blockedAccessTiers)
    {
        // NoAccess are permanently blocked
        AccessTier[] allBlockedAccessTiers = [.. blockedAccessTiers, AccessTier.NoAccess];

        var validator = CreateValidator();
        var serviceName = new ServiceName("status");
        var serviceUri = new ServiceUri("/");

        _config.AccessControls = new Dictionary<string, PlexSsoConfig.AccessControl[]>()
        {
            {
                "status", new[]
                {
                    new PlexSsoConfig.AccessControl()
                    {
                        Path = "/",
                        MinimumAccessTier = allowedAccessTiers.Last(),
                        ControlType = PlexSsoConfig.ControlType.Allow,
                        BlockMessage = "You do not have permission to access this page.",
                    }
                }
            }
        };

        foreach (var blockedAccessTier in allBlockedAccessTiers)
        {
            var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, AccessTier = blockedAccessTier };
            var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);
            AssertSsoResponse(result, new SsoResponse(
                success: true,
                loggedIn: true,
                blocked: true,
                accessTier: AccessTier.NoAccess,
                status: 403,
                message: blockedAccessTier == AccessTier.NoAccess 
                    ? _config.DefaultAccessDeniedMessage
                    : _config.AccessControls.Values.First().First().BlockMessage
            ));
        }

        foreach (var allowedAccessTier in allowedAccessTiers)
        {
            var identity = new Identity(new List<Claim>()) { IsAuthenticated = true, AccessTier = allowedAccessTier };
            var result = validator.ValidateAuthenticationStatus(identity, serviceName, serviceUri);
            AssertSsoResponse(result, new SsoResponse(
                success: true,
                loggedIn: true,
                blocked: false,
                accessTier: allowedAccessTier,
                status: 200,
                message: Constants.DefaultAuthorizedMessage
            ));
        }
    }

    private AuthenticationValidator CreateValidator()
    {
        return new AuthenticationValidator(new TestConfigurationService(_config!), _mockLogger);
    }

    private void AssertSsoResponse(SsoResponse expected, SsoResponse actual)
    {
        Assert.That(actual.Success, Is.EqualTo(expected.Success));
        Assert.That(actual.LoggedIn, Is.EqualTo(expected.LoggedIn));
        Assert.That(actual.AccessBlocked, Is.EqualTo(expected.AccessBlocked));
        Assert.That(actual.Tier, Is.EqualTo(expected.Tier));
        Assert.That(actual.Status, Is.EqualTo(expected.Status));
        Assert.That(actual.Message, Is.EqualTo(expected.Message));
    }
}