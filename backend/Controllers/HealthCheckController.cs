using Microsoft.AspNetCore.Mvc;
using PlexSSO.Model;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        public BasicResponse Get()
        {
            return new BasicResponse(true);
        }
    }
}
