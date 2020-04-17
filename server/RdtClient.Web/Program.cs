using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RdtClient.Service.Services;

namespace RdtClient.Web
{
    public class Program
    {
        public static async Task Main(String[] args)
        {
            var configuration = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json", true, false)
                                .Build();

            var hostUrl = configuration["HostUrl"];

            if (String.IsNullOrWhiteSpace(hostUrl))
            {
                hostUrl = "http://0.0.0.0:6500";
            }

            await Host.CreateDefaultBuilder(args)
                      .UseWindowsService()
                      .ConfigureServices((hostContext, services) =>
                      {
                          services.AddHostedService<TaskRunner>();
                      })
                      .ConfigureWebHostDefaults(webBuilder =>
                      {
                          webBuilder.ConfigureLogging(logging =>
                                    {
                                        logging.ClearProviders();
                                        logging.AddConsole();
                                        logging.AddFile($"{AppContext.BaseDirectory}app.log");
                                    })
                                    .UseUrls(hostUrl)
                                    .UseKestrel()
                                    .UseStartup<Startup>();
                      })
                      .Build()
                      .RunAsync();
        }
    }

    /*public class Program
    {
        public static void Main(String[] args)
        {




            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                              .CaptureStartupErrors(false)
                              
                              .UseStartup<Startup>()
                              .UseKestrel();
                });
        }
    }*/
}