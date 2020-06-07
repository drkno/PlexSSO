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
                        {"--preferences", "preferences"},
                        {"-c", "cookie_domain"},
                        {"--cookie-domain", "cookie_domain"},
                        {"--config", "config"}
                    });
                })
                .ConfigureKestrel((context, options) => options.AddServerHeader = false)
                .UseStartup<Startup>()
                .UseUrls("http://0.0.0.0:4200/")
                .Build()
                .Run();
        }
    }
}
