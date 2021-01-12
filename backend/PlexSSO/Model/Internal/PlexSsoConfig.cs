using System.Collections.Generic;
using System.Linq;
using PlexSSO.Model.Types;

namespace PlexSSO.Model.Internal
{
    public class PlexSsoConfig
    {
        [CliArgument("config")]
        public string ConfigFile { get; set; } = "config.json";

        [CliArgument("server", "s")]
        public ServerIdentifier ServerIdentifier { get; set; } = null;

        [CliArgument("preferences", "p")]
        public string PlexPreferencesFile { get; set; } = null;

        [CliArgument("cookie_domain", "c")]
        public string CookieDomain { get; set; } = ".example.com";
        public string DefaultAccessDeniedMessage { get; set; } = "Access Denied";
        public IDictionary<string, AccessControl[]> AccessControls { get; set; } = new Dictionary<string, AccessControl[]>()
        {
            { "example-service", new[] { new AccessControl() { Exempt = new[] { new Username("some-exempt-user")  } } } }
        };
        public string OmbiPublicHostname { get; set; } = "";
        public string TautulliPublicHostname { get; set; } = "";
        public string OverseerrPublicHostname { get; set; } = "";

        public override string ToString()
        {
            var accessControls = string.Join('\n', AccessControls.Select(control => $"{control.Key}: [\n"
                + string.Join("\n\t", control.Value.Select(ctrl => ctrl.ToString())) + "\n]"));
            return $"ServerIdentifier = {ServerIdentifier}\n" +
                   $"PlexPreferencesFile = {PlexPreferencesFile}\n" +
                   $"CookieDomain = {CookieDomain}\n" +
                   $"AccessControls = {{\n{accessControls}\n}}\n" +
                   $"DefaultAccessDeniedMessage = {DefaultAccessDeniedMessage}\n" +
                   $"OmbiPublicHostname = {OmbiPublicHostname}";
        }

        public class AccessControl
        {
            public string Path { get; set; } = "/";
            public AccessTier? MinimumAccessTier { get; set; } = AccessTier.NormalUser;
            public ControlType ControlType { get; set; } = ControlType.Block;
            public Username[] Exempt { get; set; } = new Username[0];
            public string BlockMessage { get; set; } = "Access Denied";

            public override string ToString()
            {
                return $"Path = {Path}\n" +
                    $"MinimumAccessTier = {MinimumAccessTier}\n" +
                    $"ControlType = {ControlType}\n" +
                    $"Exempt = [{string.Join(',', (IEnumerable<Username>) Exempt)}]\n" +
                    $"BlockMessage = {BlockMessage}";
            }
        }

        public enum ControlType
        {
            Block,
            Allow
        }
    }
}
