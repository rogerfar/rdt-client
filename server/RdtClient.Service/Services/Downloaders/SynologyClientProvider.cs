using Synology.Api.Client;

namespace RdtClient.Service.Services.Downloaders;

/// <summary>
///     Holds a single authenticated <see cref="ISynologyClient" /> shared across all Download Station downloads.
///     Logging in once instead of once per download avoids the concurrent-login session churn that made DSM return
///     error 119 "SID not found" under high file-count concurrency. The session is created on demand, reused while
///     the credentials are unchanged, and dropped (via <see cref="InvalidateAsync" />) when a download hits a session
///     error so the next caller logs in again.
/// </summary>
public static class SynologyClientProvider
{
    private static readonly Func<String, String, String, Task<ISynologyClient>> DefaultClientFactory = async (url, username, password) =>
    {
        var client = new SynologyClient(url);
        await client.LoginAsync(username, password);

        return client;
    };

    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static ISynologyClient? _client;
    private static (String Url, String Username, String Password)? _key;

    /// <summary>
    ///     Builds an authenticated client. Overridable so tests can supply a fake without a live DSM. Restored by
    ///     <see cref="Reset" />.
    /// </summary>
    public static Func<String, String, String, Task<ISynologyClient>> ClientFactory { get; set; } = DefaultClientFactory;

    /// <summary>
    ///     Returns the shared authenticated client for these credentials, logging in once. Concurrent first calls
    ///     collapse to a single login; later calls reuse the cached client.
    /// </summary>
    public static async Task<ISynologyClient> GetClientAsync(String url, String username, String password)
    {
        var key = (url, username, password);

        // Fast path: a client for these exact credentials already exists.
        var current = _client;

        if (current != null && _key == key)
        {
            return current;
        }

        await Gate.WaitAsync();

        try
        {
            if (_client != null && _key == key)
            {
                return _client;
            }

            // No client yet, or the credentials changed: drop any stale session and authenticate fresh.
            await TryLogoutAsync(_client);

            _client = await ClientFactory(url, username, password);
            _key = key;

            return _client;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    ///     Drops the cached session so the next <see cref="GetClientAsync" /> authenticates again. Pass the client that
    ///     just failed so a session another caller has already refreshed is not thrown away.
    /// </summary>
    public static async Task InvalidateAsync(ISynologyClient? failedClient = null)
    {
        await Gate.WaitAsync();

        try
        {
            if (failedClient != null && !ReferenceEquals(_client, failedClient))
            {
                return;
            }

            await TryLogoutAsync(_client);

            _client = null;
            _key = null;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    ///     Clears the cached session and restores the default factory. For test isolation.
    /// </summary>
    public static void Reset()
    {
        _client = null;
        _key = null;
        ClientFactory = DefaultClientFactory;
    }

    private static async Task TryLogoutAsync(ISynologyClient? client)
    {
        if (client == null)
        {
            return;
        }

        try
        {
            await client.LogoutAsync();
        }
        catch
        {
            // Best-effort: an expired or dead session cannot be logged out, and that is fine.
        }
    }
}
