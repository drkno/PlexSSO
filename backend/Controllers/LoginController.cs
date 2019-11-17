using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlexSSO.Model;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> logger;
        private readonly IPlexClient plexClient;
        private readonly ServerIdentifier serverIdentifier;

        public LoginController(ILogger<LoginController> logger,
                               IPlexClient plexClient,
                               IConfiguration configuration)
        {
            this.logger = logger;
            this.plexClient = plexClient;

            var id = configuration["server"];
            if (id == null)
            {
                var pref = configuration["preferences"];
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
                var tier = User.Claims.Where(x => x.Type == Constants.AccessTierClaim)
                                      .FirstOrDefault();
                var accessTier = AccessTier.NoAccess;
                if (tier == null)
                {
                    accessTier = await plexClient.GetAccessTier(serverIdentifier, token);
                }
                else
                {
                    accessTier = (AccessTier) Enum.Parse(typeof(AccessTier), tier.Value);
                }

                if (accessTier == AccessTier.Failure)
                {
                    Response.StatusCode = 401;
                    return new SsoResponse(true, false, AccessTier.NoAccess);
                }

                var claims = new List<Claim>
                {
                    new Claim(Constants.AccessTierClaim, accessTier.ToString()),
                    new Claim(Constants.AccessTokenClaim, token.Value),
                    new Claim(Constants.ServerIdentifierClaim, serverIdentifier.Value)
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

                if (accessTier == AccessTier.NoAccess)
                {
                    Response.StatusCode = 403;
                }
                return new SsoResponse(true, true, accessTier);
            }
            catch (Exception e)
            {
                logger.LogError("Failed to log user in", e);
                Response.StatusCode = 400;
                return new SsoResponse(false, false, AccessTier.NoAccess);
            }
        }
    }
}
