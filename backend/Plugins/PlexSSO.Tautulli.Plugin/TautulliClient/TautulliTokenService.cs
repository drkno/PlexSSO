using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlexSSO.Model;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using PlexSSO.Service;
using PlexSSO.Service.Config;
using PlexSSO.Tautulli.Plugin.Model;

namespace PlexSSO.Tautulli.Plugin.TautulliClient
{
    public class TautulliTokenService : ITokenService
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        private readonly IConfigurationService _configurationService;

        public TautulliTokenService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public bool Matches((Protocol, string, string) redirectComponents)
        {
            var (_, hostname, _) = redirectComponents;
            return GetHostname().Contains(hostname);
        }

        public async Task<AuthenticationToken> GetServiceToken(Identity identity)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var httpClient = new HttpClient(handler);

            var hostname = GetHostname();

            var request = new HttpRequestMessage(HttpMethod.Post, hostname + "/auth/signin");
            request.Content = new StringContent(
                $"username=&password=&token={identity.AccessToken.Value}&remember_me=1",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "PlexSSO/2");
            request.Headers.Add("X-Api-Key", GetApiKey());

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tautulliResponse = JsonSerializer.Deserialize<TautulliTokenResponse>(json, _options);

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

        private string GetHostname()
        {
            return ThrowIfNullOrEmpty(
                _configurationService
                    .GetPluginConfig<TautulliConfig>(TautulliConstants.PluginName)
                    .PublicHostname,
                "Public Hostname"
            );
        }

        private string GetApiKey()
        {
            return ThrowIfNullOrEmpty(
                _configurationService
                    .GetPluginConfig<TautulliConfig>(TautulliConstants.PluginName)
                    .ApiKey,
                "API Key"
            );
        }

        private string ThrowIfNullOrEmpty(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Tautulli {name} is not configured.");
            }

            return value;
        }
    }
}
