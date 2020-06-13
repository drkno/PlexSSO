using System;
using System.Collections.Generic;
using System.Linq;
using PlexSSO.Service.PlexClient;

namespace PlexSSO.Service.Config
{
    public class PlexSsoConfig
    {
        public string ServerIdentifier { get; set; } = null;
        public string PlexPreferencesFile { get; set; } = null;
        public string CookieDomain { get; set; } = ".example.com";
        public string DefaultAccessDeniedMessage { get; set; } = "Access Denied";
        public IDictionary<string, AccessControl[]> AccessControls { get; set; } = new Dictionary<string, AccessControl[]>()
        {
            { "example-service", new AccessControl[] { new AccessControl() { Exempt = new string[] { "some-exempt-user" } } } }
        };
        public string OmbiPublicHostname { get; set; } = "";
        public string TautulliPublicHostname { get; set; } = "";

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
            public string[] Exempt { get; set; } = new string[0];
            public string BlockMessage { get; set; } = "Access Denied";

            public override string ToString()
            {
                return $"Path = {Path}\n" +
                    $"MinimumAccessTier = {MinimumAccessTier}\n" +
                    $"ControlType = {ControlType}\n" +
                    $"Exempt = [{string.Join(',', Exempt)}]\n" +
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
