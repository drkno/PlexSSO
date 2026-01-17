using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PlexSSO.Service.Config;
using IPlugin = PlexSSO.Plugin.IPlugin;

namespace PlexSSO.Service.Plugin
{
    public static class PluginLoader
    {
        public static void LoadPlugins(IConfigurationService configurationService,
                                       IServiceCollection serviceCollection,
                                       IMvcBuilder controllersWithViews)
        {
            var pluginDirectory = configurationService.Config.PluginDirectory;
            foreach (var plugin in Directory.EnumerateFiles(pluginDirectory, "*.Plugin.dll", SearchOption.TopDirectoryOnly))
            {
                var pluginPath = Path.Combine(pluginDirectory, plugin);
                Console.WriteLine($"Loading {pluginPath}");
                LoadPlugins(pluginPath, serviceCollection, configurationService, controllersWithViews);
            }
        }

        private static void LoadPlugins(string plugin,
                                        IServiceCollection serviceCollection,
                                        IConfigurationService configurationService,
                                        IMvcBuilder controllersWithViews)
        {
            var assembly = Assembly.LoadFile(plugin);
            controllersWithViews.AddApplicationPart(assembly);
            var types = assembly.GetTypes()
                .Where(aType => aType.IsAssignableTo(typeof(IPlugin)));

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type) as IPlugin;
                instance.RegisterServices(serviceCollection);
                instance.ConfigurePlugin(configurationService);
                Console.WriteLine($"Loaded plugin {instance.Name}");
            }
        }
    }
}
