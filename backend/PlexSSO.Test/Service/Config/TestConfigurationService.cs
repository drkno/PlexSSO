using System;
using System.Collections.Generic;
using PlexSSO.Extensions;
using PlexSSO.Model.Internal;
using PlexSSO.Service.Config;

namespace PlexSSO.Test.Service.Config;

public class TestConfigurationService(
    PlexSsoConfig config) : IConfigurationService
{
    private readonly IDictionary<string, object> _config = new Dictionary<string, object>();
    
    public PlexSsoConfig Config { get; } = config;
    public string GetConfigurationDirectory()
    {
        return "/";
    }
    public T GetPluginConfig<T>(string key)
    {
        if (!_config.TryGetValue(key, out var value))
        {
            return Activator.CreateInstance<T>();
        }
        
        return (T) value;
    }

    public void SavePluginConfig<T>(string key, T value)
    {
        _config[key] = value;
    }
}
