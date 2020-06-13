using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace PlexSSO.Service.Config
{
    public class ConfigurationService : IConfigurationService
    {
        private const string ConfigurationDirectoryKey = "config";
        private const string ConfigurationFileName = "config.json";

        private const string ServerConfigurationKey = "server";
        private const string PreferencesConfigurationKey = "preferences";
        private const string CookieDomainConfigurationKey = "cookie_domain";

        private readonly string _configDirectory;
        private readonly PlexSsoConfig _config;

        public ConfigurationService(IConfiguration configuration)
        {
            _configDirectory = configuration[ConfigurationDirectoryKey] ?? Environment.CurrentDirectory;
            var configFile = Path.Join(_configDirectory, ConfigurationFileName);
            _config = LoadConfig(configFile, configuration);
        }

        private PlexSsoConfig LoadConfig(string configFile, IConfiguration configuration)
        {
            var serialiserConfig = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            PlexSsoConfig plexSsoConfig;
            if (File.Exists(configFile))
            {
                plexSsoConfig = JsonSerializer.Deserialize<PlexSsoConfig>(File.ReadAllText(configFile), serialiserConfig);
                UpdateConfigWithCliOptions(ref plexSsoConfig, configuration);
            }
            else
            {
                plexSsoConfig = new PlexSsoConfig();
                UpdateConfigWithCliOptions(ref plexSsoConfig, configuration);
                File.WriteAllText(configFile, JsonSerializer.Serialize<PlexSsoConfig>(plexSsoConfig, serialiserConfig));
            }

            return plexSsoConfig;
        }

        private void UpdateConfigWithCliOptions(ref PlexSsoConfig plexSsoConfig, IConfiguration configuration)
        {   
            string server, preferences, cookieDomain;
            if ((server = configuration[ServerConfigurationKey]) != null)
            {
                plexSsoConfig.ServerIdentifier = server;
            }
            if ((preferences = configuration[PreferencesConfigurationKey]) != null)
            {
                plexSsoConfig.PlexPreferencesFile = preferences;
            }
            if ((cookieDomain = configuration[CookieDomainConfigurationKey]) != null)
            {
                plexSsoConfig.CookieDomain = cookieDomain;
            }
        }

        public PlexSsoConfig GetConfig()
        {
            return _config;
        }

        public PlexSsoConfig.AccessControl[] GetAccessControls(string serviceName)
        {
            if (!_config.AccessControls.TryGetValue(serviceName, out var accessControls))
            {
                accessControls = new PlexSsoConfig.AccessControl[0];
            }
            return accessControls;
        }

        public string GetConfigurationDirectory()
        {
            return _configDirectory;
        }

        public string GetOmbiUrl()
        {
            return _config.OmbiPublicHostname;
        }
    }
}

