using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using PlexSSO.Extensions;
using PlexSSO.Model;

namespace PlexSSO.Service.Config
{
    public class ConfigurationService<T> : IConfigurationService<T>
    {
        public T Config { get; }
        private readonly string _configDirectory;

        public ConfigurationService(IConfiguration configuration)
        {
            _configDirectory = configuration[Constants.ConfigurationDirectoryKey] ?? Environment.CurrentDirectory;
            var configFile = Path.Join(_configDirectory, Constants.ConfigurationFileName);
            Config = LoadConfig(configFile, configuration);
        }

        public string GetConfigurationDirectory()
        {
            return _configDirectory;
        }

        private T LoadConfig(string configFile, IConfiguration configuration)
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

            T config;
            if (File.Exists(configFile))
            {
                config = JsonSerializer.Deserialize<T>(File.ReadAllText(configFile), serialiserConfig);
                UpdateConfigWithCliOptions(ref config, configuration);
            }
            else
            {
                config = Activator.CreateInstance<T>();
                UpdateConfigWithCliOptions(ref config, configuration);
                File.WriteAllText(configFile, JsonSerializer.Serialize(config, serialiserConfig));
            }

            return config;
        }

        private void UpdateConfigWithCliOptions(ref T config, IConfiguration configuration)
        {
            foreach (var (property, cliArgument) in typeof(T).GetAnnotatedCliArgumentsAsEnumerable())
            {
                object configVal = configuration[cliArgument.Identifier];
                if (configVal != null)
                {
                    if (property.PropertyType != configVal.GetType())
                    {
                        configVal = Activator.CreateInstance(property.PropertyType, configVal);
                    }
                    property.SetValue(config, configVal);
                }
            }
        }
    }
}

