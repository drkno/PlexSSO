using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlexSSO.Model;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class SsoController : ControllerBase
    {
        private ILogger<SsoController> _logger;

        public SsoController(ILogger<SsoController> logger)
        {
            this._logger = logger;
        }

        [HttpGet]
        public SsoResponse Get()
        {
            try
            {
                var accessTier = User.Claims.FirstOrDefault(x => x.Type == Constants.AccessTierClaim);
                var sso = new SsoResponse(
                    true,
                    accessTier != null,
                    accessTier != null
                        ? (AccessTier) Enum.Parse(typeof(AccessTier), accessTier.Value)
                        : AccessTier.NoAccess
                );
                Response.StatusCode = sso.LoggedIn ? 200 : 403;
                return sso;
            }
            catch (Exception)
            {
                Response.StatusCode = 403;
                return new SsoResponse(true, false, AccessTier.NoAccess);
            }
        }
    }
}
