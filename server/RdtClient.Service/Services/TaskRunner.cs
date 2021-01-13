using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RdtClient.Service.Services
{
    public class TaskRunner : BackgroundService
    {
        private readonly ILogger<TaskRunner> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TaskRunner(ILogger<TaskRunner> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            using var scope = _serviceProvider.CreateScope();
            var torrentRunner = scope.ServiceProvider.GetRequiredService<ITorrentRunner>();
            
            _logger.LogInformation("TaskRunner started.");

            await torrentRunner.Initialize();
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await torrentRunner.Tick();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in TorrentDownloadManager.Tick");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }

            _logger.LogInformation("TaskRunner stopped.");
        }
    }
}