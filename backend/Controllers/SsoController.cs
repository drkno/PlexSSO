using Microsoft.AspNetCore.Mvc;
using PlexSSO.Model;
using PlexSSO.Service.Auth;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class SsoController : CommonAuthController
    {
        private readonly IAuthValidator _authValidator;

        public SsoController(IAuthValidator authValidator)
        {
            this._authValidator = authValidator;
        }

        [HttpGet]
        public SsoResponse Get()
        {
            var (accessTier, loggedIn) = GetAccessTier();
            var serviceName = GetServiceName();
            var serviceUri = GetServiceUri();
            var userName = GetUserName();

            var response = _authValidator.ValidateAuthenticationStatus(accessTier, loggedIn, serviceName, serviceUri, userName);
            Response.StatusCode = response.Status;
            return response;
        }
    }
}
