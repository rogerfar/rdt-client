using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RdtClient.Web
{
    public class Program
    {
        public static void Main(String[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        private static IHostBuilder CreateHostBuilder(String[] args)
        {
            return Host
                   .CreateDefaultBuilder(args)
                   .ConfigureLogging(logging =>
                   {
                       logging.AddFile("app.log");
                   })
                   .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }
}