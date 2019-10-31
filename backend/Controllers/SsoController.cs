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
        public BasicResponse Get()
        {
            var loggedIn = User.Claims.Where(x =>
                x.Type == Constants.AccessTierClaim &&
                x.Value != AccessTier.NoAccess.ToString()).Any();
            Response.StatusCode = loggedIn ? 200 : 403;
            return new BasicResponse(loggedIn);
        }
    }
}
