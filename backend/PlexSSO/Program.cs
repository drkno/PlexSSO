using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using PlexSSO.Extensions;
using PlexSSO.Model;
using PlexSSO.Model.Internal;

namespace PlexSSO
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddCommandLine(args, typeof(PlexSsoConfig).GetAnnotatedCliArgumentsAsDictionary());
                })
                .ConfigureKestrel((context, options) => options.AddServerHeader = false)
                .UseStartup<Startup>()
                .UseUrls($"http://0.0.0.0:{Constants.PortNumber}/")
                .Build()
                .Run();
        }
    }
}
