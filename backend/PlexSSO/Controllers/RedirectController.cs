using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlexSSO.Extensions;
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
        private static readonly Regex SubdomainRegex =
            new("^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$", RegexOptions.Compiled);

        private readonly IConfigurationService _configurationService;
        private readonly IEnumerable<ITokenService> _tokenServices;
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(IConfigurationService configurationService,
                                  IEnumerable<ITokenService> tokenServices,
                                  ILogger<RedirectController> logger)
        {
            _configurationService = configurationService;
            _tokenServices = tokenServices;
            _logger = logger;
        }

        [HttpGet("{*location}")]
        public async Task<BasicResponse> Get(string location)
        {
            // The first path segment becomes the subdomain of the redirect target. Anything other than a
            // plain DNS label (e.g. "evil.com#" or "evil.com?") would let the host portion escape our domain.
            if (!IsValidSubdomain(location?.Split('/')[0]))
            {
                // Deliberately not logging the value: it is attacker controlled
                _logger.LogWarning("Rejected redirect with an invalid subdomain");
                Response.StatusCode = 400;
                return new BasicResponse(false);
            }

            var redirectComponents = GetRedirectComponents(location);
            int redirectStatus;

            var service = _tokenServices.FirstOrDefault(tokenService => tokenService.Matches(redirectComponents));

            AuthenticationToken authToken;
            if (service != null && (authToken = await service.GetServiceToken(Identity)) != null)
            {
                redirectStatus = GenerateRedirect(authToken, redirectComponents);
            }
            else
            {
                redirectStatus = GenerateNormalRedirectUrl(redirectComponents);
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
            var redirectSuccess = Response.Headers.TryAdd("Location", protoString + host + location);
            Response.Cookies.AppendWithoutEncoding(authenticationToken.CookieName, authenticationToken.CookieValue, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Expires = authenticationToken.Expires,
                Domain = _configurationService.Config.CookieDomain,
                Path = "/",
                Secure = false
            });
            _logger.LogInformation("Performing service specific redirect to '{location}' with status {status}. Added Location header = {added}",
                StripLineBreaks(location), authenticationToken.StatusCode, redirectSuccess);
            return authenticationToken.StatusCode;
        }

        private int GenerateNormalRedirectUrl((Protocol, string, string) redirectComponents)
        {
            var (protocol, host, path) = redirectComponents;
            var protoString = protocol == Protocol.Https ? "https://" : "http://";
            var location = protoString + host + path;
            var redirectSuccess = Response.Headers.TryAdd("Location", location);
            _logger.LogInformation("Attempting redirect to '{location}' with status 302. Added Location header = {added}",
                StripLineBreaks(location), redirectSuccess);
            return 302;
        }

        // The path and query string are user controlled; prevent them forging additional log entries
        private static string StripLineBreaks(string value)
        {
            return value?.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }

        public static bool IsValidSubdomain(string subdomain)
        {
            return !string.IsNullOrEmpty(subdomain) && SubdomainRegex.IsMatch(subdomain);
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
            if (spl.Length < 3)
            {
                host = "." + Request.Host.Host;
            }
            else
            {
                host = Request.Host.Host.Substring(Request.Host.Host.IndexOf('.'));
            }
            return subdomain + host + port;
        }
    }
}
