using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Services;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

namespace RdtClient.Web
{
    public class Program
    {
        public static async Task Main(String[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();

                // Perform migrations
                using (var scope = host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                    await dbContext.Database.MigrateAsync();
                    await dbContext.Seed();
                }
                
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static IHostBuilder CreateHostBuilder(String[] args)
        {
            var configuration = new ConfigurationBuilder()
#if DEBUG
                                .AddJsonFile("appsettings.Development.json", true, false)
#else
                                .AddJsonFile("appsettings.json", true, false)
#endif
                                .Build();

            var appSettings = new AppSettings();
            configuration.Bind(appSettings);

            if (String.IsNullOrWhiteSpace(appSettings.HostUrl))
            {
                appSettings.HostUrl = "http://0.0.0.0:6500";
            }

            if (!Enum.TryParse(appSettings.Logging.LogLevel.Default, out LogEventLevel logLevel))
            {
                logLevel = LogEventLevel.Information;
            }
            
            Log.Logger = new LoggerConfiguration()
                         .Enrich.FromLogContext()
                         .Enrich.WithExceptionDetails()
                         .WriteTo.File(appSettings.Logging.File.Path, logLevel, rollOnFileSizeLimit: true, fileSizeLimitBytes: appSettings.Logging.File.FileSizeLimitBytes, retainedFileCountLimit: appSettings.Logging.File.MaxRollingFiles)
                         .MinimumLevel.Information()
                         .CreateLogger();

            Serilog.Debugging.SelfLog.Enable(msg =>
            {
                Debug.Print(msg);
                Debugger.Break();
                Console.WriteLine(msg);
                Debug.WriteLine(msg);
            });

            return Host.CreateDefaultBuilder(args)
                       .UseWindowsService()
                       .ConfigureServices((hostContext, services) =>
                       {
                           services.AddHostedService<TaskRunner>();
                       })
                       .ConfigureWebHostDefaults(webBuilder =>
                       {
                           webBuilder.UseUrls(appSettings.HostUrl)
                                     .UseSerilog()
                                     .UseKestrel()
                                     .UseStartup<Startup>();
                       });
        }
    }
}
