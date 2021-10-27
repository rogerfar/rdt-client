using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RdtClient.Service.Services
{
    public class Startup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public Startup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var settings = scope.ServiceProvider.GetRequiredService<Settings>();

            await settings.ResetCache();
            
            await settings.Clean();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
