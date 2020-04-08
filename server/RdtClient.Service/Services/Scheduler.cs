using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using RdtClient.Data.Enums;

namespace RdtClient.Service.Services
{
    public interface IScheduler
    {
        void Start();
        Task Process();
    }

    public class Scheduler : IScheduler
    {
        private readonly IDownloads _downloads;
        private readonly ISettings _settings;
        private readonly ITorrents _torrents;

        public Scheduler(ITorrents torrents, IDownloads downloads, ISettings settings)
        {
            _torrents = torrents;
            _downloads = downloads;
            _settings = settings;
        }

        public void Start()
        {
            RecurringJob.AddOrUpdate(() => Process(), "* * * * *");
            BackgroundJob.Enqueue(() => Process());
        }

        [DisableConcurrentExecution(5)]
        public async Task Process()
        {
            await _torrents.Update();

            var downloads = await _downloads.Get();

            downloads = downloads.Where(m => m.Status != DownloadStatus.Finished)
                                    .OrderByDescending(m => m.Status)
                                    .ThenByDescending(m => m.Added)
                                    .ToList();

            var maxDownloads = await _settings.GetNumber("DownloadLimit");
            var destinationFolderPath = await _settings.GetString("DownloadFolder");

            foreach (var download in downloads)
            {
                if (DownloadManager.ActiveDownloads.Count >= maxDownloads)
                {
                    return;
                }

                download.Torrent = null;
                BackgroundJob.Enqueue(() => DownloadManager.Download(download, destinationFolderPath));

                await Task.Delay(1000);
            }
        }
    }
}