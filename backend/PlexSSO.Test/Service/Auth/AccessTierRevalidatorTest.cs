using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using PlexSSO.Service.Auth;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Test.Service.Auth;

public class AccessTierRevalidatorTest
{
    private IPlexClient _plexClient = null!;
    private MemoryCache _cache = null!;
    private AccessTierRevalidator _revalidator = null!;

    [SetUp]
    public void Setup()
    {
        _plexClient = Substitute.For<IPlexClient>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _revalidator = CreateRevalidator(requireTwoFactor: false);
    }

    private AccessTierRevalidator CreateRevalidator(bool requireTwoFactor, params string[] exempt)
    {
        return new AccessTierRevalidator(_plexClient,
            TwoFactorPolicyFactory.Create(_plexClient, requireTwoFactor, exempt), _cache,
            Substitute.For<ILogger<AccessTierRevalidator>>());
    }

    [TearDown]
    public void TearDown() => _cache.Dispose();

    private static CookieValidatePrincipalContext CreateContext(AccessTier tier)
    {
        var identity = new Identity([])
        {
            UserIdentifier = new UserIdentifier("uuid"),
            AccessTier = tier,
            AccessToken = new AccessToken("token"),
            ServerIdentifier = new ServerIdentifier("server"),
            Username = new Username("user"),
            DisplayName = new DisplayName("User"),
            Email = new Email("user@example.com")
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(identity.AsClaims(), CookieAuthenticationDefaults.AuthenticationScheme));

        var authService = Substitute.For<IAuthenticationService>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(authService).BuildServiceProvider()
        };
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null,
            typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);
        return new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket);
    }

    [Test]
    public async Task ValidatePrincipal_WhenTierUnchanged_KeepsPrincipal()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.NormalUser);
        var context = CreateContext(AccessTier.NormalUser);
        var original = context.Principal;

        await _revalidator.ValidatePrincipal(context);

        Assert.That(context.Principal, Is.SameAs(original));
        Assert.That(context.ShouldRenew, Is.False);
    }

    [Test]
    public async Task ValidatePrincipal_WhenAccessRemoved_RejectsPrincipal()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.NoAccess);
        var context = CreateContext(AccessTier.NormalUser);

        await _revalidator.ValidatePrincipal(context);

        Assert.That(context.Principal, Is.Null);
    }

    [Test]
    public async Task ValidatePrincipal_WhenTierChanged_ReplacesTier()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.NormalUser);
        var context = CreateContext(AccessTier.HomeUser);

        await _revalidator.ValidatePrincipal(context);

        Assert.That(new Identity(context.Principal!.Claims).AccessTier, Is.EqualTo(AccessTier.NormalUser));
        Assert.That(context.ShouldRenew, Is.True);
    }

    [Test]
    public async Task ValidatePrincipal_WhenPlexUnavailable_KeepsExistingTier()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.Failure);
        var context = CreateContext(AccessTier.Owner);
        var original = context.Principal;

        await _revalidator.ValidatePrincipal(context);

        Assert.That(context.Principal, Is.SameAs(original));
    }

    [Test]
    public async Task ValidatePrincipal_CachesResultBetweenRequests()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.Owner);

        await _revalidator.ValidatePrincipal(CreateContext(AccessTier.Owner));
        await _revalidator.ValidatePrincipal(CreateContext(AccessTier.Owner));

        await _plexClient.ReceivedWithAnyArgs(1).GetAccessTier(default!, default!);
    }

    [Test]
    public async Task ValidatePrincipal_WhenTwoFactorNotRequired_DoesNotCheckIt()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.NormalUser);

        await _revalidator.ValidatePrincipal(CreateContext(AccessTier.NormalUser));

        await _plexClient.DidNotReceiveWithAnyArgs().IsTwoFactorEnabled(default!);
    }

    [Test]
    public async Task ValidatePrincipal_WhenTwoFactorRequiredAndDisabled_RejectsPrincipal()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.NormalUser);
        _plexClient.IsTwoFactorEnabled(default!).ReturnsForAnyArgs(false);
        var context = CreateContext(AccessTier.NormalUser);

        await CreateRevalidator(requireTwoFactor: true).ValidatePrincipal(context);

        Assert.That(context.Principal, Is.Null);
    }

    [Test]
    public async Task ValidatePrincipal_WhenTwoFactorRequiredButUserExempt_KeepsPrincipal()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.NormalUser);
        _plexClient.IsTwoFactorEnabled(default!).ReturnsForAnyArgs(false);
        var context = CreateContext(AccessTier.NormalUser);
        var original = context.Principal;

        await CreateRevalidator(requireTwoFactor: true, "user").ValidatePrincipal(context);

        Assert.That(context.Principal, Is.SameAs(original));
    }

    [Test]
    public async Task ValidatePrincipal_WhenTwoFactorStatusUnavailable_KeepsExistingTier()
    {
        _plexClient.GetAccessTier(default!, default!).ReturnsForAnyArgs(AccessTier.NormalUser);
        _plexClient.IsTwoFactorEnabled(default!).ReturnsForAnyArgs((bool?)null);
        var context = CreateContext(AccessTier.NormalUser);
        var original = context.Principal;

        await CreateRevalidator(requireTwoFactor: true).ValidatePrincipal(context);

        Assert.That(context.Principal, Is.SameAs(original));
    }
}
