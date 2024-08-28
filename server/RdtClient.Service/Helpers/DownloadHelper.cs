using System;
using System.Web;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;

namespace RdtClient.Service.Helpers;

public static class DownloadHelper
{
    public static async Task<String?> GetDownloadPath(String downloadPath, Torrent torrent, Download download)
    {
        var fileUrl = download.Link;

        if (String.IsNullOrWhiteSpace(fileUrl) || torrent.RdName == null)
        {
            return null;
        }

        var directory = RemoveInvalidPathChars(torrent.RdName);

        var uri = new Uri(fileUrl);
        var torrentPath = Path.Combine(downloadPath, directory);

        var fileName = uri.Segments.Last();

        if (Settings.Get.Provider.Provider == Provider.TorBox)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, uri);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.Content.Headers.ContentDisposition != null)
                {
                    fileName = response.Content.Headers.ContentDisposition.FileName!.Trim('"');
                }
            }
        }

        fileName = HttpUtility.UrlDecode(fileName);

        fileName = FileHelper.RemoveInvalidFileNameChars(fileName);

        var matchingTorrentFiles = torrent.Files.Where(m => m.Path.EndsWith(fileName)).Where(m => !String.IsNullOrWhiteSpace(m.Path)).ToList();

        if (matchingTorrentFiles.Count > 0)
        {
            var matchingTorrentFile = matchingTorrentFiles[0];

            var subPath = Path.GetDirectoryName(matchingTorrentFile.Path);

            if (!String.IsNullOrWhiteSpace(subPath))
            {
                subPath = subPath.Trim('/').Trim('\\');

                torrentPath = Path.Combine(torrentPath, subPath);
            }
        }

        if (!Directory.Exists(torrentPath))
        {
            Directory.CreateDirectory(torrentPath);
        }

        var filePath = Path.Combine(torrentPath, fileName);

        Console.WriteLine($"FILEPATH HERE {filePath}");

        return filePath;
    }

    public static async Task<String?> GetDownloadPath(Torrent torrent, Download download)
    {
        var fileUrl = download.Link;

        if (String.IsNullOrWhiteSpace(fileUrl) || torrent.RdName == null)
        {
            return null;
        }

        var uri = new Uri(fileUrl);
        var torrentPath = RemoveInvalidPathChars(torrent.RdName);

        var fileName = uri.Segments.Last();

        if (Settings.Get.Provider.Provider == Provider.TorBox)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, uri);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.Content.Headers.ContentDisposition != null)
                {
                    fileName = response.Content.Headers.ContentDisposition.FileName!.Trim('"');
                }
            }
        }

        fileName = HttpUtility.UrlDecode(fileName);

        fileName = FileHelper.RemoveInvalidFileNameChars(fileName);

        var matchingTorrentFiles = torrent.Files.Where(m => m.Path.EndsWith(fileName)).Where(m => !String.IsNullOrWhiteSpace(m.Path)).ToList();

        if (matchingTorrentFiles.Count > 0)
        {
            var matchingTorrentFile = matchingTorrentFiles[0];

            var subPath = Path.GetDirectoryName(matchingTorrentFile.Path);

            if (!String.IsNullOrWhiteSpace(subPath))
            {
                subPath = subPath.Trim('/').Trim('\\');

                torrentPath = Path.Combine(torrentPath, subPath);
            }
        }

        if (!Directory.Exists(torrentPath))
        {
            Directory.CreateDirectory(torrentPath);
        }

        var filePath = Path.Combine(torrentPath, fileName);

        return filePath;
    }

    private static String RemoveInvalidPathChars(String path)
    {
        return String.Concat(path.Split(Path.GetInvalidPathChars()));
    }
}