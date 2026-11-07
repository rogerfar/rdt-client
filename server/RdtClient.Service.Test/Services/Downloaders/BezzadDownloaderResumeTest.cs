using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using RdtClient.Service.Services;
using RdtClient.Service.Services.Downloaders;
using Xunit.Abstractions;

namespace RdtClient.Service.Test.Services.Downloaders;

// Changes the static Settings, so keep it out of parallel runs.
[CollectionDefinition(nameof(BezzadDownloaderResumeCollection), DisableParallelization = true)]
public class BezzadDownloaderResumeCollection;

[Collection(nameof(BezzadDownloaderResumeCollection))]
public sealed class BezzadDownloaderResumeTest : IDisposable
{
    private const Int32 FileSize = 8 * 1024 * 1024;

    private readonly String _directory = Path.Combine(Path.GetTempPath(), $"rdt-resume-{Guid.NewGuid():N}");
    private readonly Byte[] _payload = RandomNumberGenerator.GetBytes(FileSize);
    private readonly RangeFileServer _server;

    private readonly ITestOutputHelper _output;

    public BezzadDownloaderResumeTest(ITestOutputHelper output)
    {
        _output = output;
        Directory.CreateDirectory(_directory);
        _server = new(_payload);
    }

    public void Dispose()
    {
        _server.Dispose();

        try
        {
            Directory.Delete(_directory, true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task StoppedDownload_ResumesFromThePartialFile()
    {
        var settings = Settings.Get.DownloadClient;
        var oldSpeed = settings.MaxSpeed;
        var filePath = Path.Combine(_directory, "file.bin");

        try
        {
            // Slow the first attempt down so it can be stopped part way through.
            settings.MaxSpeed = 2;

            var first = new BezzadDownloader(_server.Url, filePath);
            var firstBytesDone = 0L;
            var halfway = new TaskCompletionSource();
            var firstDone = new TaskCompletionSource<String?>();

            first.DownloadProgress += (_, args) =>
            {
                Interlocked.Exchange(ref firstBytesDone, args.BytesDone);

                if (args.BytesDone >= FileSize / 3)
                {
                    halfway.TrySetResult();
                }
            };
            first.DownloadComplete += (_, args) => firstDone.TrySetResult(args.Error);

            await first.Download();
            await halfway.Task.WaitAsync(TimeSpan.FromSeconds(30));

            // Resume metadata is written at most once a second while downloading.
            await Task.Delay(TimeSpan.FromMilliseconds(1500));

            await first.Cancel();
            await firstDone.Task.WaitAsync(TimeSpan.FromSeconds(30));

            var stoppedAt = Interlocked.Read(ref firstBytesDone);
            Assert.True(stoppedAt < FileSize, "The first attempt finished before it could be stopped.");
            Assert.True(File.Exists(filePath + ".download"), "The partial .download file was not kept.");

            // Second attempt, as after a restart: a new downloader for the same path, at full speed.
            settings.MaxSpeed = 0;
            _server.ResetCounters();

            var second = new BezzadDownloader(_server.Url, filePath);
            var secondDone = new TaskCompletionSource<String?>();
            second.DownloadComplete += (_, args) => secondDone.TrySetResult(args.Error);

            await second.Download();
            var error = await secondDone.Task.WaitAsync(TimeSpan.FromSeconds(60));

            _output.WriteLine($"File {FileSize} bytes, stopped after {stoppedAt}, resumed attempt fetched {_server.BodyBytesSent}.");

            Assert.Null(error);
            Assert.Equal(SHA256.HashData(_payload), SHA256.HashData(await File.ReadAllBytesAsync(filePath)));

            // Only the part that was missing should have been fetched again (with some slack for
            // data received after the last metadata write and for chunk boundaries).
            Assert.True(_server.BodyBytesSent < FileSize - FileSize / 4,
                        $"Resumed download fetched {_server.BodyBytesSent} of {FileSize} bytes again; first attempt had {stoppedAt}.");
        }
        finally
        {
            settings.MaxSpeed = oldSpeed;
        }
    }

    private sealed class RangeFileServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly Byte[] _payload;
        private Int64 _bodyBytesSent;

        public RangeFileServer(Byte[] payload)
        {
            _payload = payload;

            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint) probe.LocalEndpoint).Port;
            probe.Stop();

            Url = $"http://127.0.0.1:{port}/file.bin";
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            _ = Task.Run(AcceptLoop);
        }

        public String Url { get; }

        public Int64 BodyBytesSent => Interlocked.Read(ref _bodyBytesSent);

        public void ResetCounters()
        {
            Interlocked.Exchange(ref _bodyBytesSent, 0);
        }

        public void Dispose()
        {
            _listener.Close();
        }

        private async Task AcceptLoop()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                _ = Task.Run(() => Serve(context));
            }
        }

        private async Task Serve(HttpListenerContext context)
        {
            try
            {
                var start = 0L;
                var end = _payload.Length - 1L;
                var range = context.Request.Headers["Range"];

                if (range != null && range.StartsWith("bytes="))
                {
                    var parts = range["bytes=".Length..].Split('-');
                    start = Int64.Parse(parts[0]);

                    if (parts[1].Length > 0)
                    {
                        end = Math.Min(Int64.Parse(parts[1]), end);
                    }

                    context.Response.StatusCode = 206;
                    context.Response.AddHeader("Content-Range", $"bytes {start}-{end}/{_payload.Length}");
                }

                context.Response.AddHeader("Accept-Ranges", "bytes");
                context.Response.ContentLength64 = end - start + 1;

                if (context.Request.HttpMethod != "HEAD")
                {
                    // Write in small pieces so a client that stops reading is only charged for what it took.
                    for (var offset = start; offset <= end; offset += 64 * 1024)
                    {
                        var count = (Int32) Math.Min(64 * 1024, end - offset + 1);
                        await context.Response.OutputStream.WriteAsync(_payload.AsMemory((Int32) offset, count));
                        Interlocked.Add(ref _bodyBytesSent, count);
                    }
                }

                context.Response.Close();
            }
            catch (Exception)
            {
                // The client went away (a stopped download), which is expected.
                try
                {
                    context.Response.Abort();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
