using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlexSSO.Model;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RedirectController : ControllerBase
    {
        private ILogger<RedirectController> _logger;

        public RedirectController(ILogger<RedirectController> logger)
        {
            _logger = logger;
        }

        private string GetRawHostPort()
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
            return host + port;
        }

        private string GetRedirectHostAndPath(string location)
        {
            var spl = location.Split('/');
            var host = spl[0] + GetRawHostPort();
            var path = "/" + string.Join('/', spl[1..]);
            var queryString = Request.QueryString.Value;
            return "https://" + host + path + queryString;
        }

        [HttpGet("{*location}")]
        public BasicResponse Get(string location)
        {           
            Response.StatusCode = 302;
            Response.Headers.Add("Location", GetRedirectHostAndPath(location));
            return new BasicResponse(true);
        }
    }
}
