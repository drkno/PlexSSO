using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PlexSSO
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddCommandLine(args, new Dictionary<string, string>() {
                        {"-s", "server"},
                        {"--server", "server"},
                        {"-p", "preferences"},
                        {"--preferences", "preferences"}
                    });
                })
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
