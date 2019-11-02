using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PlexSSO.Model;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class SsoController : ControllerBase
    {
        [HttpGet]
        public SsoResponse Get()
        {
            var accessTier = User.Claims.FirstOrDefault(x => x.Type == Constants.AccessTierClaim);
            var sso = new SsoResponse(
                accessTier == null,
                accessTier == null
                    ? (AccessTier) Enum.Parse(typeof(AccessTier), accessTier.Value)
                    : AccessTier.NoAccess
            );
            Response.StatusCode = sso.Success ? 200 : 403;
            return sso;
        }
    }
}
