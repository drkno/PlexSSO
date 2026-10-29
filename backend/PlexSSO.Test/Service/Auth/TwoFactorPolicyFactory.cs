using System.Linq;
using NSubstitute;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using PlexSSO.Service.Auth;
using PlexSSO.Service.Config;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Test.Service.Auth;

public static class TwoFactorPolicyFactory
{
    public static TwoFactorPolicy Create(IPlexClient plexClient, bool requireTwoFactor, params string[] exempt)
    {
        var configurationService = Substitute.For<IConfigurationService>();
        configurationService.Config.Returns(new PlexSsoConfig
        {
            RequireTwoFactorAuthentication = requireTwoFactor,
            TwoFactorExempt = exempt.Select(u => new Username(u)).ToArray()
        });
        return new TwoFactorPolicy(configurationService, plexClient);
    }
}
