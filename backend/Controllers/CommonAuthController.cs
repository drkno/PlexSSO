using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PlexSSO.Model;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Controllers
{
    public abstract class CommonAuthController : ControllerBase
    {
        protected T GetClaim<T>(string claim, T defaultValue, Func<string, T> mappingFunc)
        {
            try
            {
                return User.Claims
                    .Where(x => x.Type == claim)
                    .Select(x => mappingFunc(x.Value))
                    .DefaultIfEmpty(defaultValue)
                    .First();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        protected string GetClaim(string claim, string defaultValue)
        {
            return GetClaim(claim, defaultValue, str => str);
        }

        protected (AccessTier, bool) GetAccessTier()
        {
            return GetClaim(Constants.AccessTierClaim, (AccessTier.NoAccess, false), str =>
            {
                var tier = (AccessTier) Enum.Parse(typeof(AccessTier), str);
                return (tier, true);
            });
        }

        protected string GetUserName()
        {
            return GetClaim(Constants.UsernameClaim, string.Empty);
        }

        protected PlexToken GetAuthenticatedUserToken()
        {
            var token = GetClaim(Constants.AccessTokenClaim, null);
            return token == null ? null : new PlexToken(token);
        }

        protected string GetProtocol()
        {            
            var headerValue = GetFirstHeader(
                Constants.UpstreamProtocolHeader,
                Constants.ForwardedProtoHeader,
                Constants.FrontEndHttpsHeader,
                Constants.ForwardedProtocolHeader,
                Constants.ForwardedSslHeader,
                Constants.UrlSchemeHeader
            );
            switch (headerValue)
            {
                case "on":
                case "https":
                    return "https://";
                case "off":
                case "http":
                    return "http://";
                default:
                    goto case "on";
            }
        }

        protected string GetFirstHeader(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (HttpContext.Request.Headers.ContainsKey(key))
                {
                    return HttpContext.Request.Headers[key][0];
                }
            }
            return null;
        }

        private string GetHeader(string key, string defaultValue)
        {
            if (!HttpContext.Request.Headers.TryGetValue(key, out var headerValue))
            {
                headerValue = defaultValue;
            }
            return headerValue;
        }

        protected string GetServiceName()
        {
            return GetHeader(Constants.SsoServiceHeader, string.Empty);
        }

        protected string GetServiceUri()
        {
            return GetHeader(Constants.SsoOrigionalUriHeader, string.Empty);
        }
    }
}
