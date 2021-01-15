using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlexSSO.Model.API;
using PlexSSO.Model.Internal;
using PlexSSO.Model.Types;
using PlexSSO.Service;
using PlexSSO.Service.Config;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RedirectController : CommonAuthController
    {
        private readonly IConfigurationService<PlexSsoConfig> _configurationService;
        private readonly IEnumerable<ITokenService> _tokenServices;

        public RedirectController(IConfigurationService<PlexSsoConfig> configurationService,
                                  IEnumerable<ITokenService> tokenServices)
        {
            _configurationService = configurationService;
            _tokenServices = tokenServices;
        }

        [HttpGet("{*location}")]
        public async Task<BasicResponse> Get(string location)
        {           
            var redirectComponents = GetRedirectComponents(location);
            int redirectStatus;

            var service = _tokenServices.FirstOrDefault(tokenService => tokenService.Matches(redirectComponents));

            if (service == null)
            {
                redirectStatus = GenerateNormalRedirectUrl(redirectComponents);
            }
            else
            {
                var authToken = await service.GetServiceToken(Identity);
                redirectStatus = GenerateRedirect(authToken, redirectComponents);
            }

            Response.StatusCode = redirectStatus;
            return new BasicResponse(true);
        }

        private int GenerateRedirect(AuthenticationToken authenticationToken,
                                     (Protocol, string, string) redirectComponents)
        {
            var (protocol, host, path) = redirectComponents;
            var protoString = protocol == Protocol.Https ? "https://" : "http://";
            var location = string.IsNullOrWhiteSpace(authenticationToken.Location)
                ? path
                : authenticationToken.Location;
            Response.Headers.Add("Location", protoString + host + location);
            Response.Cookies.Append(authenticationToken.CookieName, authenticationToken.CookieValue, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Expires = authenticationToken.Expires,
                Domain = _configurationService.Config.CookieDomain,
                Path = "/",
                Secure = false
            });
            return authenticationToken.StatusCode;
        }

        private int GenerateNormalRedirectUrl((Protocol, string, string) redirectComponents)
        {
            var (protocol, host, path) = redirectComponents;
            var protoString = protocol == Protocol.Https ? "https://" : "http://";
            Response.Headers.Add("Location", protoString + host + path);
            return 302;
        }

        private (Protocol, string, string) GetRedirectComponents(string location)
        {
            var spl = location.Split('/');
            var host = GetHostWithPort(spl[0]);
            var path = "/" + string.Join('/', spl[1..]) + Request.QueryString.Value;
            return (Protocol, host, path);
        }

        private string GetHostWithPort(string subdomain)
        {
            var port = Request.Host.Port.HasValue ? $":{Request.Host.Port.Value}" : "";
            var spl = Request.Host.Host.Split('.');
            string host;
            if (spl.Length < 2)
            {
                host = "." + Request.Host.Host;
                return subdomain + host + port;
            }
            else
            {
                host = Request.Host.Host.Substring(Request.Host.Host.IndexOf('.'));
            }
            return host + port;
        }
    }
}
