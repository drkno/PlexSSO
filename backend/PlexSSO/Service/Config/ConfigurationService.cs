using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using PlexSSO.Extensions;
using PlexSSO.Model;
using PlexSSO.Model.Internal;

namespace PlexSSO.Service.Config;

public class ConfigurationService : IConfigurationService
{
    private static readonly JsonSerializerOptions SerializerConfig = new JsonSerializerOptions
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
    
    private readonly IConfiguration _configuration;
    private readonly string _configDirectory;
    private readonly Dictionary<string, object> _pluginConfigs;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _configDirectory = configuration[Constants.ConfigurationDirectoryKey] ?? Environment.CurrentDirectory;
        var configFile = Path.Join(_configDirectory, Constants.ConfigurationFileName);
        Config = LoadConfig<PlexSsoConfig>(configFile, configuration);
        _pluginConfigs = new Dictionary<string, object>();
    }

    public PlexSsoConfig Config { get; }

    public string GetConfigurationDirectory()
    {
        return _configDirectory;
    }

    public T GetPluginConfig<T>(string key)
    {
        if (_pluginConfigs.TryGetValue(key, out var config))
        {
            return (T)config;
        }
        var configFile = Path.Join(_configDirectory, key + Constants.ConfigurationFileExt);
        var pluginConfig = LoadConfig<T>(configFile, _configuration);
        _pluginConfigs[key] = pluginConfig;
        return pluginConfig;
    }

    public void SavePluginConfig<T>(string key, T value)
    {
        _pluginConfigs[key] = value;
        var configFile = Path.Join(_configDirectory, key + Constants.ConfigurationFileExt);
        SaveConfig(configFile, value);
    }

    private static T LoadConfig<T>(string configFile, IConfiguration configuration)
    {
        var config = File.Exists(configFile)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(configFile), SerializerConfig)
            : Activator.CreateInstance<T>();

        UpdateConfigWithCliOptions(ref config, configuration);
        SaveConfig(configFile, config);

        return config;
    }
    
    private static void SaveConfig<T>(string configFile, T config)
    {
        File.WriteAllText(configFile, JsonSerializer.Serialize(config, SerializerConfig), Encoding.UTF8);
    }

    private static void UpdateConfigWithCliOptions<T>(ref T config, IConfiguration configuration)
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