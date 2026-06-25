using System.Net;
using System.Net.Sockets;
using Downloader;
using RdtClient.Service.Helpers;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class BezzadDownloader : IDownloader
{
    private const Int32 BindRetryDelayMilliseconds = 1000;

    private readonly DownloadConfiguration _downloadConfiguration;
    private readonly DownloadService _downloadService;
    private readonly IDelayProvider _delayProvider;
    private readonly String _filePath;
    private readonly ILogger _logger;
    private readonly String _uri;

    private Boolean _finished;

    public BezzadDownloader(String uri, String filePath)
        : this(uri, filePath, null)
    {
    }

    internal BezzadDownloader(String uri, String filePath, IDelayProvider? delayProvider)
    {
        _logger = Log.ForContext<BezzadDownloader>();
        _logger.Debug($"Instantiated new Bezzad Downloader for URI {uri} to filePath {filePath}");

        _uri = uri;
        _filePath = filePath;
        _delayProvider = delayProvider ?? new DefaultDelayProvider();

        // For all options, see https://github.com/bezzad/Downloader
        _downloadConfiguration = new()
        {
            MaxTryAgainOnFailure = 5,
            RangeDownload = false,
            ClearPackageOnCompletionWithFailure = true,
            CheckDiskSizeBeforeDownload = false,
            MaximumMemoryBufferBytes = 1024 * 1024 * 10,
            CustomHttpMessageHandlerFactory = CreateHttpMessageHandler,
            RequestConfiguration =
            {
                Accept = "*/*",
                UserAgent = $"rdt-client",
                ProtocolVersion = HttpVersion.Version11,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                KeepAlive = true,
                UseDefaultCredentials = false
            }
        };

        SetSettings();

        _downloadService = new(_downloadConfiguration);

        _downloadService.DownloadProgressChanged += (_, args) =>
        {
            if (DownloadProgress == null)
            {
                return;
            }

            DownloadProgress.Invoke(this,
                                    new()
                                    {
                                        Speed = (Int64)args.BytesPerSecondSpeed,
                                        BytesDone = args.ReceivedBytesSize,
                                        BytesTotal = args.TotalBytesToReceive
                                    });
        };

        _downloadService.DownloadFileCompleted += (_, args) =>
        {
            String? error = null;

            if (args.Cancelled)
            {
                error = $"The download was cancelled";
            }
            else if (args.Error != null)
            {
                error = args.Error.Message;
            }

            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = error
                                     });

            _finished = true;
        };
    }

    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    public Task<String> Download()
    {
        _logger.Debug($"Starting download of {_uri}, writing to path: {_filePath}");

        _ = RunDownloadAsync();
        _ = StartTimer();

        return Task.FromResult(Guid.NewGuid().ToString());
    }

    public Task Cancel()
    {
        _logger.Debug($"Cancelling download {_uri}");

        _finished = true;
        _downloadService.CancelAsync();

        return Task.CompletedTask;
    }

    public Task Pause()
    {
        _logger.Debug($"Pausing download {_uri}");
        _downloadService.Pause();

        return Task.CompletedTask;
    }

    public Task Resume()
    {
        _logger.Debug($"Resuming download {_uri}");
        _downloadService.Resume();

        return Task.CompletedTask;
    }

    private SocketsHttpHandler CreateHttpMessageHandler()
    {
        var proxy = CreateProxy();

        var handler = new SocketsHttpHandler
        {
            Proxy = proxy,
            UseProxy = proxy != null
        };

        if (Settings.Get.DownloadClient.BindToSpecificIp)
        {
            handler.ConnectCallback = ConnectCallback;
        }

        return handler;
    }

    private async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bindIpAddress = GetBindIpAddress();

            if (bindIpAddress == null)
            {
                _logger.Warning("Bezzad bind-to-IP is enabled but the configured IP address is invalid or empty. Retrying.");
                await _delayProvider.Delay(BindRetryDelayMilliseconds);

                continue;
            }

            try
            {
                return await ConnectWithBindingAsync(context, bindIpAddress, cancellationToken);
            }
            catch (SocketException ex)
            {
                _logger.Warning(ex, "Unable to bind Bezzad download to IP {BindIpAddress} for {Uri}. Retrying.", bindIpAddress, _uri);
            }
            catch (HttpRequestException ex)
            {
                _logger.Warning(ex, "Unable to bind Bezzad download to IP {BindIpAddress} for {Uri}. Retrying.", bindIpAddress, _uri);
            }

            await _delayProvider.Delay(BindRetryDelayMilliseconds);
        }
    }

    private static WebProxy? CreateProxy()
    {
        var settingProxyServer = Settings.Get.DownloadClient.ProxyServer;

        if (String.IsNullOrWhiteSpace(settingProxyServer))
        {
            return null;
        }

        return new(new Uri(settingProxyServer), false);
    }

    private async Task RunDownloadAsync()
    {
        try
        {
            await _downloadService.DownloadFileTaskAsync(_uri, _filePath);
        }
        catch (OperationCanceledException) when (_finished)
        {
        }
        catch (Exception ex)
        {
            if (_finished)
            {
                return;
            }

            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = ex.Message
                                     });

            _finished = true;
        }
    }

    private async ValueTask<Stream> ConnectWithBindingAsync(SocketsHttpConnectionContext context, IPAddress bindIpAddress, CancellationToken cancellationToken)
    {
        var remoteAddresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host);

        var compatibleRemoteAddresses = remoteAddresses.Where(m => m.AddressFamily == bindIpAddress.AddressFamily).ToArray();

        if (compatibleRemoteAddresses.Length == 0)
        {
            throw new SocketException((Int32)SocketError.HostNotFound);
        }

        Exception? lastError = null;

        foreach (var remoteAddress in compatibleRemoteAddresses)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await ConnectToRemoteAddressAsync(remoteAddress, context.DnsEndPoint.Port, bindIpAddress, cancellationToken);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw lastError ?? new HttpRequestException($"Unable to connect to {context.DnsEndPoint.Host}:{context.DnsEndPoint.Port}");
    }

    private static async ValueTask<Stream> ConnectToRemoteAddressAsync(IPAddress remoteAddress, Int32 remotePort, IPAddress? bindIpAddress, CancellationToken cancellationToken)
    {
        var socket = new Socket(remoteAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            if (bindIpAddress != null)
            {
                socket.Bind(new IPEndPoint(bindIpAddress, 0));
            }

            await socket.ConnectAsync(new IPEndPoint(remoteAddress, remotePort), cancellationToken);

            return new NetworkStream(socket, true);
        }
        catch
        {
            socket.Dispose();

            throw;
        }
    }

    private IPAddress? GetBindIpAddress()
    {
        return BindIpAddressHelper.TryResolve(Settings.Get.DownloadClient.BindToSpecificIp,
                                              Settings.Get.DownloadClient.BindIpAddress);
    }

    private void SetSettings()
    {
        var settingDownloadMaxSpeed = Settings.Get.DownloadClient.MaxSpeed;

        if (settingDownloadMaxSpeed <= 0)
        {
            settingDownloadMaxSpeed = 0;
        }

        settingDownloadMaxSpeed = settingDownloadMaxSpeed * 1024 * 1024;

        var settingDownloadTimeout = Settings.Get.DownloadClient.Timeout;

        if (settingDownloadTimeout <= 0)
        {
            settingDownloadTimeout = 1000;
        }

        var settingParallelCount = Settings.Get.DownloadClient.ParallelCount;

        if (settingParallelCount <= 0)
        {
            settingParallelCount = 4;
        }

        if (Settings.Get.DownloadClient.ChunkCount <= 0)
        {
            _downloadConfiguration.ChunkCount = 8;
        }
        else
        {
            _downloadConfiguration.ChunkCount = Settings.Get.DownloadClient.ChunkCount;
        }

        _downloadConfiguration.MaximumBytesPerSecond = settingDownloadMaxSpeed;
        _downloadConfiguration.ParallelDownload = settingParallelCount > 1;
        _downloadConfiguration.ParallelCount = settingParallelCount;
        _downloadConfiguration.BlockTimeout = settingDownloadTimeout;

        // Bind-to-IP connect retries can run indefinitely; HttpClientTimeout must not cut them off.
        _downloadConfiguration.HttpClientTimeout = Settings.Get.DownloadClient.BindToSpecificIp ? Int32.MaxValue : settingDownloadTimeout;
    }

    private async Task StartTimer()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync())
        {
            if (_finished)
            {
                return;
            }

            SetSettings();
        }
    }

    private sealed class DefaultDelayProvider : IDelayProvider
    {
        public Task Delay(Int32 milliseconds)
        {
            return Task.Delay(milliseconds);
        }
    }
}
