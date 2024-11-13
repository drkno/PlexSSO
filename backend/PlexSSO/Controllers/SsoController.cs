using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PlexSSO.Model;
using PlexSSO.Model.API;
using PlexSSO.Service.Auth;

namespace PlexSSO.Controllers
{
    [ApiController]
    [Route(Constants.ControllerPath)]
    public class SsoController : CommonAuthController
    {
        private readonly ILogger<SsoController> _logger;
        private readonly IAuthValidator _authValidator;

        public SsoController(
            ILogger<SsoController> logger,
            IAuthValidator authValidator)
        {
            _logger = logger;
            _authValidator = authValidator;
        }

        [HttpGet]
        public SsoResponse Get()
        {
            var response = _authValidator.ValidateAuthenticationStatus(Identity, ServiceName, ServiceUri);
            Response.StatusCode = response.Status;

            if (Identity.IsAuthenticated)
            {
                var success = Response.Headers.TryAdd(Constants.SsoResponseUserHeader, Identity.Username.ToString());
                success = success || Response.Headers.TryAdd(Constants.SsoResponseEmailHeader, Identity.Email.ToString());

                if (!success)
                {
                    _logger.LogWarning("Adding response headers failed:\n{headers}", Response.Headers);
                }
            }
            return response;
        }
    }
}
