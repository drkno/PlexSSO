using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlexSSO.Service.Config;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Service.OmbiClient
{
    public class OmbiTokenService : IOmbiTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService _configurationService;

        public OmbiTokenService(IHttpClientFactory clientFactory,
                                IConfigurationService configurationService)
        {
            _httpClient = clientFactory.CreateClient();
            _configurationService = configurationService;
        }

        public async Task<OmbiToken> GetOmbiToken(PlexToken plexToken)
        {
            
            var request = new HttpRequestMessage(HttpMethod.Post, _configurationService.GetOmbiUrl() + "/api/v1/token/plextoken");
            request.Content = new StringContent($"{{\"plexToken\":\"{plexToken.Value}\"}}", Encoding.UTF8, "application/json");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var ombiResponse = JsonSerializer.Deserialize<OmbiTokenResponse>(json, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
                PropertyNameCaseInsensitive = true
            });

            return string.IsNullOrWhiteSpace(ombiResponse.AccessToken)
                ? null
                : new OmbiToken(ombiResponse.AccessToken);
        }

        // https://github.com/YohDeadfall/corefx/blob/5ef51a3e6bb0ee752264c81a7e9496bda958619d/src/System.Text.Json/src/System/Text/Json/Serialization/JsonSnakeCaseNamingPolicy.cs
        private class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;

                // Allocates a string builder with the guessed result length,
                // where 5 is the average word length in English, and
                // max(2, length / 5) is the number of underscores.
                StringBuilder builder = new StringBuilder(name.Length + Math.Max(2, name.Length / 5));
                UnicodeCategory? previousCategory = null;

                for (int currentIndex = 0; currentIndex < name.Length; currentIndex++)
                {
                    char currentChar = name[currentIndex];
                    if (currentChar == '_')
                    {
                        builder.Append('_');
                        previousCategory = null;
                        continue;
                    }

                    UnicodeCategory currentCategory = char.GetUnicodeCategory(currentChar);

                    switch (currentCategory)
                    {
                        case UnicodeCategory.UppercaseLetter:
                        case UnicodeCategory.TitlecaseLetter:
                            if (previousCategory == UnicodeCategory.SpaceSeparator ||
                                previousCategory == UnicodeCategory.LowercaseLetter ||
                                previousCategory != UnicodeCategory.DecimalDigitNumber &&
                                currentIndex > 0 &&
                                currentIndex + 1 < name.Length &&
                                char.IsLower(name[currentIndex + 1]))
                            {
                                builder.Append('_');
                            }

                            currentChar = char.ToLower(currentChar);
                            break;

                        case UnicodeCategory.LowercaseLetter:
                        case UnicodeCategory.DecimalDigitNumber:
                            if (previousCategory == UnicodeCategory.SpaceSeparator)
                            {
                                builder.Append('_');
                            }
                            break;

                        case UnicodeCategory.Surrogate:
                            break;

                        default:
                            if (previousCategory != null)
                            {
                                previousCategory = UnicodeCategory.SpaceSeparator;
                            }
                            continue;
                    }

                    builder.Append(currentChar);
                    previousCategory = currentCategory;
                }

                return builder.ToString();
            }
        }
    }
}

