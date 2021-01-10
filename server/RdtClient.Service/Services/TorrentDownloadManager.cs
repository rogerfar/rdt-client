using System.Threading.Tasks;

namespace RdtClient.Service.Services
{
    public interface ITorrentDownloadManager
    {
        Task Tick();
    }
    
    public class TorrentDownloadManager : ITorrentDownloadManager
    {
        public async Task Tick()
        {
            throw new System.NotImplementedException();
        }
    }
}
