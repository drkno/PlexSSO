using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlexSSO.Model;
using PlexSSO.Service.Auth;
using PlexSSO.Service.Config;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class SsoController : ControllerBase
    {
        private const string SsoServiceHeader = "X-PlexSSO-For";
        private const string SsoOrigionalUriHeader = "X-PlexSSO-Original-URI";
        private readonly IConfigurationService _configurationService;
        private readonly IAuthValidator _authValidator;
        private readonly ILogger<SsoController> _logger;

        public SsoController(IConfigurationService configurationService,
                             IAuthValidator authValidator,
                             ILogger<SsoController> logger)
        {
            this._configurationService = configurationService;
            this._authValidator = authValidator;
            this._logger = logger;
        }

        [HttpGet]
        public SsoResponse Get()
        {
            try
            {
                var sso = ApplyAccessRules();
                Response.StatusCode = sso.AccessBlocked ? 403 : 200;
                return sso;
            }
            catch (Exception)
            {
                Response.StatusCode = 403;
                return new SsoResponse(true, false, false, AccessTier.NoAccess);
            }
        }

        private (AccessTier, bool) GetAccessTier()
        {
            try
            {
                return User.Claims
                    .Where(x => x.Type == Constants.AccessTierClaim)
                    .Select(x => (AccessTier) Enum.Parse(typeof(AccessTier), x.Value))
                    .Select(x => (x, true))
                    .DefaultIfEmpty((AccessTier.NoAccess, false))
                    .First();
            }
            catch (Exception)
            {
                return (AccessTier.NoAccess, false);
            }
        }

        private string GetUserName()
        {
            try
            {
                return User.Claims
                    .Where(x => x.Type == Constants.UsernameClaim)
                    .Select(x => x.Value)
                    .DefaultIfEmpty("")
                    .First();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string GetHeader(string key, string defaultValue)
        {
            if (!HttpContext.Request.Headers.TryGetValue(key, out var headerValue))
            {
                headerValue = defaultValue;
            }
            return headerValue;
        }

        private string GetServiceName()
        {
            return GetHeader(SsoServiceHeader, string.Empty);
        }

        private string GetServiceUri()
        {
            return GetHeader(SsoOrigionalUriHeader, string.Empty);
        }

        private SsoResponse ApplyAccessRules()
        {
            var (accessTier, loggedIn) = GetAccessTier();
            var serviceName = GetServiceName();
            var serviceUri = GetServiceUri();
            var userName = GetUserName();

            return _authValidator.ValidateAuthenticationStatus(accessTier, loggedIn, serviceName, serviceUri, userName);
        }
    }
}
