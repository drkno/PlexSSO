using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlexSSO.Model;
using PlexSSO.Model.API;
using PlexSSO.Model.Types;
using PlexSSO.Service.Auth;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route(Constants.ControllerPath)]
    public class LoginController : CommonAuthController
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IPlexClient _plexClient;
        private readonly IAuthValidator _authValidator;
        private readonly TwoFactorPolicy _twoFactorPolicy;
        private readonly ServerIdentifier _serverIdentifier;

        public LoginController(ILogger<LoginController> logger,
                               IPlexClient plexClient,
                               IAuthValidator authValidator,
                               TwoFactorPolicy twoFactorPolicy)
        {
            _logger = logger;
            _plexClient = plexClient;
            _authValidator = authValidator;
            _twoFactorPolicy = twoFactorPolicy;
            _serverIdentifier = plexClient.GetPlexServerIdentifier();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<SsoResponse> Login([FromBody] LoginPost data)
        {
            try
            {
                Identity.AccessToken = new AccessToken(data.Token);
                Identity.ServerIdentifier = _serverIdentifier;

                // Always set the AccessTier even if already logged in, as the user may have changed since last login
                Identity.AccessTier = await _plexClient.GetAccessTier(_serverIdentifier, Identity.AccessToken);

                if (Identity.AccessTier == AccessTier.Failure)
                {
                    Identity.AccessTier = AccessTier.NoAccess;
                    Identity.IsAuthenticated = false;
                    return GetErrorResponse();
                }

                var user = await _plexClient.GetUserInfo(Identity.AccessToken);
                Identity.UserIdentifier = user.UserIdentifier;
                Identity.Email = user.Email;
                Identity.Username = user.Username;
                Identity.DisplayName = user.DisplayName;
                Identity.Thumbnail = user.Thumbnail;

                var twoFactorSatisfied = await _twoFactorPolicy.IsSatisfied(Identity.Username, Identity.AccessToken);
                if (twoFactorSatisfied != true)
                {
                    _logger.LogWarning("Blocked login for {user}: two-factor authentication is required but {state}",
                        Identity.Username, twoFactorSatisfied == null ? "its status could not be retrieved" : "is not enabled");
                    Identity.AccessTier = AccessTier.NoAccess;
                    Identity.IsAuthenticated = false;
                    Response.StatusCode = 403;
                    return new SsoResponse(true, false, true, AccessTier.NoAccess, 403, _twoFactorPolicy.RequiredMessage);
                }

                Identity.IsAuthenticated = true;

                var identity = new ClaimsIdentity(
                    Identity.AsClaims(),
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

                var response = _authValidator.ValidateAuthenticationStatus(Identity, ServiceName, ServiceUri);
                Response.StatusCode = response.Status;
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to log user in: {e}", e);
                Identity.AccessTier = AccessTier.NoAccess;
                Identity.IsAuthenticated = false;
                return GetErrorResponse();
            }
        }

        private SsoResponse GetErrorResponse()
        {
            Response.StatusCode = 400;
            return new SsoResponse(false,
                Identity.IsAuthenticated,
                true,
                AccessTier.NoAccess,
                400,
                "An error occurred");
        }
    }
}
