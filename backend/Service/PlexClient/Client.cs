using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlexSSO.Service.PlexClient
{
    public class Client : IPlexClient
    {
        private readonly HttpClient _httpClient;

        public Client(IHttpClientFactory clientFactory)
        {
            _httpClient = clientFactory.CreateClient();
        }

        private async Task<IEnumerable<(string, string, string)>> GetServers(Token token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://plex.tv/api/resources");
            request.Headers.Add("includeHttps", "1");
            request.Headers.Add("includeRelay", "1");
            request.Headers.Add("X-Plex-Product", "PlexSSO");
            request.Headers.Add("X-Plex-Version", "Plex OAuth");
            request.Headers.Add("X-Plex-Client-Identifier", "PlexSSOv2");
            request.Headers.Add("X-Plex-Token", token.Value);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var xml = await response.Content.ReadAsStringAsync();
            return XDocument.Parse(xml)
                .Root
                .Elements("Device")
                .Select(x => (
                    x.Attribute("clientIdentifier").Value,
                    x.Attribute("owned")?.Value ?? "0",
                    x.Attribute("home")?.Value ?? "0"
                ));
        }

        private IEnumerable<(ServerIdentifier, AccessTier)> GetServerDetails(IEnumerable<(string, string, string)> servers)
        {
            return servers.Select(x =>
            {
                var accessLevel = AccessTier.NormalUser;
                if (x.Item2 == "1")
                {
                    accessLevel = AccessTier.Owner;
                }
                else if (x.Item3 == "1")
                {
                    accessLevel = AccessTier.HomeUser;
                }
                return (
                    new ServerIdentifier(x.Item1),
                    accessLevel
                );
            });
        }

        public ServerIdentifier GetLocalServerIdentifier(string path = "Preferences.xml")
        {
            return new ServerIdentifier(XDocument.Parse(File.ReadAllText(path)).Root.Attribute("ProcessedMachineIdentifier").Value);
        }

        public async Task<AccessTier> GetAccessTier(ServerIdentifier serverId, Token token)
        {
            try
            {
                var servers = await GetServers(token);
                var serverDetails = GetServerDetails(servers);
                var accessLevel = serverDetails.Where(x => x.Item1 == serverId)
                                               .Select(x => x.Item2)
                                               .FirstOrDefault();
                return accessLevel == default(AccessTier) ? AccessTier.NoAccess : accessLevel;
            }
            catch
            {
                return AccessTier.Failure;
            }                
        }
    }
}

