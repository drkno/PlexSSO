using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlexSSO.Service.Config;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Service.TautulliClient
{
    public class TautulliClient : ITautulliTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService _configurationService;

        public TautulliClient(IHttpClientFactory clientFactory,
                                IConfigurationService configurationService)
        {
            _httpClient = clientFactory.CreateClient();
            _configurationService = configurationService;
        }

        public async Task<TautulliToken> GetTautulliToken(PlexToken plexToken)
        {            
            var request = new HttpRequestMessage(HttpMethod.Post, _configurationService.GetTautulliUrl() + "/auth/signin");
            request.Content = new StringContent($"username=&password=&token={plexToken.Value}&remember_me=1", Encoding.UTF8, "application/x-www-form-urlencoded");
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

            return string.IsNullOrWhiteSpace(tautulliResponse.Token) || tautulliResponse.Status != "success"
                ? null
                : new TautulliToken(tautulliResponse.UUID, tautulliResponse.Token);
        }
    }
}

