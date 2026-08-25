using System;
using System.Collections.Generic;
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
            var disabledPlugins = new HashSet<string>(
                configurationService.Config.DisabledPlugins ?? [],
                StringComparer.OrdinalIgnoreCase);

            foreach (var plugin in Directory.EnumerateFiles(pluginDirectory, "*.Plugin.dll", SearchOption.TopDirectoryOnly))
            {
                var pluginPath = Path.Combine(pluginDirectory, plugin);
                Console.WriteLine($"Loading {pluginPath}");
                LoadPlugins(pluginPath, serviceCollection, configurationService, controllersWithViews, disabledPlugins);
            }
        }

        private static void LoadPlugins(string plugin,
                                        IServiceCollection serviceCollection,
                                        IConfigurationService configurationService,
                                        IMvcBuilder controllersWithViews,
                                        ISet<string> disabledPlugins)
        {
            var assembly = Assembly.LoadFile(plugin);
            var types = assembly.GetTypes()
                .Where(aType => aType.IsAssignableTo(typeof(IPlugin)));

            var instances = types
                .Select(type => Activator.CreateInstance(type) as IPlugin)
                .Where(instance => instance != null)
                .ToList();

            var enabledInstances = new List<IPlugin>();
            foreach (var instance in instances)
            {
                if (disabledPlugins.Contains(instance.Name))
                {
                    Console.WriteLine($"Skipping disabled plugin {instance.Name} ({plugin})");
                    continue;
                }
                enabledInstances.Add(instance);
            }

            if (enabledInstances.Count == 0)
            {
                // Every IPlugin discovered in this assembly is disabled - do not register its
                // application part, so none of its controllers/routes/endpoints become reachable.
                return;
            }

            controllersWithViews.AddApplicationPart(assembly);

            foreach (var instance in enabledInstances)
            {
                instance.RegisterServices(serviceCollection);
                instance.ConfigurePlugin(configurationService);
                Console.WriteLine($"Loaded plugin {instance.Name}");
            }
        }
    }
}
