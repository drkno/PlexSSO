using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlexSSO.Model;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using PlexSSO.Service.Config;

namespace PlexSSO.Service.TautulliClient
{
    public class TautulliTokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService<PlexSsoConfig> _configurationService;

        public TautulliTokenService(IHttpClientFactory clientFactory,
                                    IConfigurationService<PlexSsoConfig> configurationService)
        {
            _httpClient = clientFactory.CreateClient();
            _configurationService = configurationService;
        }

        public bool Matches((Protocol, string, string) redirectComponents)
        {
            var (_, hostname, _) = redirectComponents;
            return _configurationService.Config.TautulliPublicHostname?.Contains(hostname) ?? false;
        }

        public async Task<AuthenticationToken> GetServiceToken(Identity identity)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _configurationService.Config.TautulliPublicHostname + "/auth/signin");
            request.Content = new StringContent($"username=&password=&token={identity.AccessToken.Value}&remember_me=1",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "PlexSSO/2");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var tautulliResponse = JsonSerializer.Deserialize<TautulliTokenResponse>(json, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (string.IsNullOrWhiteSpace(tautulliResponse?.Token) || tautulliResponse.Status != "success")
            {
                return null;
            }

            return new AuthenticationToken(
                "tautulli_token_" + tautulliResponse.UUID,
                tautulliResponse.Token,
                DateTimeOffset.Now.AddDays(Constants.RedirectCookieExpireDays),
                "/home"
            );
        }
    }
}

