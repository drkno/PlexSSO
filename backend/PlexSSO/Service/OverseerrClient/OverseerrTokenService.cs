using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using PlexSSO.Model;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using PlexSSO.Service.Config;

namespace PlexSSO.Service.OverseerrClient
{
    public class OverseerrTokenService : ITokenService
    {
        private const string OverseerrCookieName = "connect.sid";

        private readonly HttpClient _httpClient;
        private readonly IConfigurationService<PlexSsoConfig> _configurationService;

        public OverseerrTokenService(IHttpClientFactory clientFactory,
                                     IConfigurationService<PlexSsoConfig> configurationService)
        {
            _httpClient = clientFactory.CreateClient();
            _configurationService = configurationService;
        }

        public bool Matches((Protocol, string, string) redirectComponents)
        {
            var (_, hostname, _) = redirectComponents;
            return _configurationService.Config.OverseerrPublicHostname?.Contains(hostname) ?? false;
        }

        public async Task<AuthenticationToken> GetServiceToken(Identity identity)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _configurationService.Config.OverseerrPublicHostname + "/api/v1/auth/login");
            request.Content = new StringContent($"{{\"authToken\":\"{identity.AccessToken.Value}\"}}", Encoding.UTF8, "application/json");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "PlexSSO/2");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var cookieString = response.Headers
                .Where(header => header.Key == "Set-Cookie")
                .Select(header => header.Value.SingleOrDefault())
                .SingleOrDefault(value => value?.Contains(OverseerrCookieName) ?? false);

            if (string.IsNullOrWhiteSpace(cookieString))
            {
                return null;
            }

            var cookie = SetCookieHeaderValue.Parse(cookieString);

            return new AuthenticationToken(
                OverseerrCookieName,
                Uri.UnescapeDataString(cookie.Value.Value),
                cookie.Expires ?? DateTimeOffset.Now.AddDays(Constants.RedirectCookieExpireDays)
            );
        }
    }
}
