using System;
using System.IO;
using System.Linq;
using System.Web;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Helpers
{
    public static class DownloadHelper
    {
        public static String GetDownloadPath(String downloadPath, Torrent torrent, Download download)
        {
            var fileUrl = download.Link;

            if (String.IsNullOrWhiteSpace(fileUrl))
            {
                return null;
            }

            var uri = new Uri(fileUrl);
            var torrentPath = Path.Combine(downloadPath, torrent.RdName);

            if (!Directory.Exists(torrentPath))
            {
                Directory.CreateDirectory(torrentPath);
            }

            var fileName = uri.Segments.Last();

            fileName = HttpUtility.UrlDecode(fileName);

            var filePath = Path.Combine(torrentPath, fileName);

            return filePath;
        }
    }
}
