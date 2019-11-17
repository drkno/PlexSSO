using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlexSSO.Model;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class LogoutController : ControllerBase
    {
        private readonly ILogger<LogoutController> logger;

        public LogoutController(ILogger<LogoutController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        public async Task<BasicResponse> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return new BasicResponse(true);
            }
            catch (Exception e)
            {
                logger.LogError("Failed to log a user out", e);
                Response.StatusCode = 400;
                return new BasicResponse(false);
            }
        }
    }
}

