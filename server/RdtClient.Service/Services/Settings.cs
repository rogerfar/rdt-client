using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aria2NET;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Services
{
    public class Settings
    {
        private readonly SettingData _settingData;

        public Settings(SettingData settingData)
        {
            _settingData = settingData;
        }

        public static DbSettings Get => SettingData.Get;

        public async Task<IList<Setting>> GetAll()
        {
            return await _settingData.GetAll();
        }

        public async Task Update(IList<Setting> settings)
        {
            await _settingData.Update(settings);
        }

        public async Task UpdateString(String key, String value)
        {
            await _settingData.UpdateString(key, value);
        }

        public async Task TestPath(String path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                throw new Exception("Path is not set");
            }

            path = path.TrimEnd('/').TrimEnd('\\');

            if (!Directory.Exists(path))
            {
                throw new Exception($"Path {path} does not exist");
            }

            var testFile = $"{path}/test.txt";

            await File.WriteAllTextAsync(testFile, "RealDebridClient Test File, you can remove this file.");
            
            await FileHelper.Delete(testFile);
        }

        public async Task<Double> TestDownloadSpeed(CancellationToken cancellationToken)
        {
            var downloadPath = Get.DownloadPath;

            var testFilePath = Path.Combine(downloadPath, "testDefault.rar");

            await FileHelper.Delete(testFilePath);

            var download = new Download
            {
                Link = "https://34.download.real-debrid.com/speedtest/testDefault.rar",
                Torrent = new Torrent
                {
                    RdName = ""
                }
            };

            var downloadClient = new DownloadClient(download, download.Torrent, downloadPath);

            await downloadClient.Start(Get);

            while (!downloadClient.Finished)
            {
#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods that take one
                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay(10);
#pragma warning restore CA2016 // Forward the 'CancellationToken' parameter to methods that take one
                
                if (cancellationToken.IsCancellationRequested)
                {
                    await downloadClient.Cancel();
                }

                if (downloadClient.BytesDone > 1024 * 1024 * 50)
                {
                    await downloadClient.Cancel();

                    break;
                }
            }

            await FileHelper.Delete(testFilePath);

            await Clean();

            return downloadClient.Speed;
        }

        public async Task<Double> TestWriteSpeed()
        {
            var downloadPath = Get.DownloadPath;

            var testFilePath = Path.Combine(downloadPath, "test.tmp");

            await FileHelper.Delete(testFilePath);

            const Int32 testFileSize = 64 * 1024 * 1024;

            var watch = new Stopwatch();

            watch.Start();

            var rnd = new Random();

            await using var fileStream = new FileStream(testFilePath, FileMode.Create, FileAccess.Write, FileShare.Write);

            var buffer = new Byte[64 * 1024];

            while (fileStream.Length < testFileSize)
            {
                rnd.NextBytes(buffer);

                await fileStream.WriteAsync(buffer.AsMemory(0, buffer.Length));
            }
            
            watch.Stop();

            var writeSpeed = fileStream.Length / watch.Elapsed.TotalSeconds;
            
            fileStream.Close();

            await FileHelper.Delete(testFilePath);

            return writeSpeed;
        }

        public async Task<VersionResult> GetAria2cVersion(String url, String secret)
        {
            var client = new Aria2NetClient(url, secret);

            return await client.GetVersion();
        }

        public async Task Clean()
        {
            try
            {
                var tempPath = Get.TempPath;

                if (!String.IsNullOrWhiteSpace(tempPath))
                {
                    var files = Directory.GetFiles(tempPath, "*.dsc", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        await FileHelper.Delete(file);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        public async Task ResetCache()
        {
            await _settingData.ResetCache();
        }
    }
}
