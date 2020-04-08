using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Web
{
    public class Program
    {
        public static void Main(String[] args)
        {
            var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", true, false)
                                .Build();

            var hostUrl = configuration["HostUrl"];

            if (String.IsNullOrWhiteSpace(hostUrl))
            {
                hostUrl = "http://0.0.0.0:6500";
            }

            WebHost.CreateDefaultBuilder(args)
                   .UseStartup<Startup>()
                   .ConfigureLogging(logging =>
                   {
                       logging.ClearProviders();
                       logging.AddConsole();
                       logging.AddFile($"{AppContext.BaseDirectory}app.log");
                   })
                   .CaptureStartupErrors(false)
                   .UseUrls(hostUrl)
                   .UseKestrel()
                   .UseIISIntegration()
                   .Build()
                   .Run();
        }
    }
}