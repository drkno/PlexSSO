using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    public class LoginController : CommonAuthController
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IPlexClient _plexClient;
        private readonly IAuthValidator _authValidator;
        private readonly ServerIdentifier serverIdentifier;

        public LoginController(ILogger<LoginController> logger,
                               IPlexClient plexClient,
                               IAuthValidator authValidator,
                               IConfigurationService configuration)
        {
            this._logger = logger;
            this._plexClient = plexClient;
            this._authValidator = authValidator;

            var id = configuration.GetConfig().ServerIdentifier;
            if (id == null)
            {
                var pref = configuration.GetConfig().PlexPreferencesFile;
                serverIdentifier = plexClient.GetLocalServerIdentifier(pref ?? "Preferences.xml");
            }
            else
            {
                serverIdentifier = new ServerIdentifier(id);
            }
        }

        [HttpPost]
        public async Task<SsoResponse> Login([FromBody] LoginPost data)
        {
            try
            {
                var token = new Token(data.Token);
                var (accessTier, loggedIn) = GetAccessTier();
                if (!loggedIn)
                {
                    accessTier = await _plexClient.GetAccessTier(serverIdentifier, token);
                }

                if (accessTier == AccessTier.Failure)
                {
                    var loginFailureResponse = _authValidator.ValidateAuthenticationStatus(AccessTier.NoAccess, false, GetServiceName(), GetServiceUri(), string.Empty);
                    Response.StatusCode = loginFailureResponse.Status;
                    return loginFailureResponse;
                }

                var user = await _plexClient.GetUserInfo(token);

                var claims = new List<Claim>
                {
                    new Claim(Constants.AccessTierClaim, accessTier.ToString()),
                    new Claim(Constants.AccessTokenClaim, token.Value),
                    new Claim(Constants.ServerIdentifierClaim, serverIdentifier.Value),
                    new Claim(Constants.UsernameClaim, user.Username),
                    new Claim(Constants.EmailClaim, user.Email),
                    new Claim(Constants.ThumbnailClaim, user.Thumbnail)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    authProperties
                );

                var response = _authValidator.ValidateAuthenticationStatus(accessTier, true, GetServiceName(), GetServiceUri(), user.Username);
                Response.StatusCode = response.Status;
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to log user in", e);
                var unhandledErrorResponse = _authValidator.ValidateAuthenticationStatus(AccessTier.NoAccess, false, GetServiceName(), GetServiceUri(), string.Empty, true);
                Response.StatusCode = unhandledErrorResponse.Status;
                return unhandledErrorResponse;
            }
        }
    }
}
