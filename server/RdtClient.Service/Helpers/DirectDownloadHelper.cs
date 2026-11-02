using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace RdtClient.Service.Helpers;

public record DirectDownloadStandIn(String PostUrl, String Title, String? Source);

public record DirectDownloadPart(String Link, IList<String> Mirrors);

public static partial class DirectDownloadHelper
{
    public const String MetaPost = "x-ddl-post";
    public const String MetaSource = "x-ddl-source";

    public const String HostPreferenceEnvVar = "DDL_HOST_PREFERENCE";

    /// <summary>
    ///     Hosts whose links are never used, comma separated (e.g. "nitroflare"): the debrid service can't download from
    ///     them reliably, so neither the host pick nor a mirror should land there.
    /// </summary>
    public const String HostBlacklistEnvVar = "DDL_HOST_BLACKLIST";
    public const String DefaultHostPreference = "rapidgator,nitroflare,ddownload,1fichier,katfile,fikper";

    private static readonly Dictionary<String, String> HostAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rg"] = "rapidgator",
        ["nitro"] = "nitroflare",
        ["ddl"] = "ddownload"
    };

    private static readonly HttpClient WebhookClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    [GeneratedRegex(@"https?://[^\s""'<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\.part0*(\d+)\.rar$", RegexOptions.IgnoreCase)]
    private static partial Regex RarPartRegex();

    [GeneratedRegex(@"\.(r\d{2,3}|z\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex OldStyleVolumeRegex();

    [GeneratedRegex(@"\.html?$", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlSuffixRegex();

    /// <summary>
    ///     Reads a placeholder NZB produced by a direct-download indexer bridge. It carries the release post URL
    ///     in its head meta instead of usenet segments. Returns null for regular NZBs.
    /// </summary>
    public static DirectDownloadStandIn? ParseStandIn(Byte[] nzb)
    {
        XDocument doc;

        try
        {
            using var stream = new MemoryStream(nzb);
            using var reader = XmlReader.Create(stream, new() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
            doc = XDocument.Load(reader);
        }
        catch (XmlException)
        {
            return null;
        }

        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var meta = doc.Root?.Elements(ns + "head").Elements(ns + "meta").ToList() ?? [];

        String? Meta(String type)
        {
            return meta.FirstOrDefault(m => m.Attribute("type")?.Value == type)?.Value.Trim();
        }

        var post = Meta(MetaPost);

        if (!Uri.TryCreate(post, UriKind.Absolute, out var postUri) || (postUri.Scheme != Uri.UriSchemeHttp && postUri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var title = Meta("name");

        if (String.IsNullOrWhiteSpace(title))
        {
            title = postUri.Segments.Last().Trim('/');
        }

        return new(postUri.ToString(), title, Meta(MetaSource));
    }

    public static String Hash(String postUrl)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(postUrl.Trim()));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    ///     Extracts the hoster links from pasted text and keeps a single mirror: release posts usually list the same
    ///     archive parts on several hosts, and downloading every mirror would fetch each part more than once.
    /// </summary>
    public static IList<String> PickMirror(String text, String? hostPreference = null)
    {
        return PlanParts(text, hostPreference).Select(p => p.Link).ToList();
    }

    /// <summary>
    ///     Same pick as <see cref="PickMirror" />, and for each picked link the same file on the other hosts, in preference
    ///     order, so a part the debrid service refuses can be fetched from another mirror. Files are matched by the name in
    ///     the hoster URL; a link without a file name gets no mirrors.
    /// </summary>
    public static IList<DirectDownloadPart> PlanParts(String text, String? hostPreference = null, String? hostBlacklist = null)
    {
        var hosts = RankHosts(text, hostPreference, hostBlacklist);

        if (hosts.Count == 0)
        {
            return [];
        }

        var others = hosts.Skip(1).SelectMany(g => g).ToList();

        return hosts[0].Select(link =>
                       {
                           var key = MirrorKey(link);

                           var mirrors = key == null ? [] : others.Where(m => MirrorKey(m) == key).ToList();

                           return new DirectDownloadPart(link, mirrors);
                       })
                       .ToList();
    }

    /// <summary>
    ///     The links of every host <see cref="PlanParts" /> did not pick, one per line, or null when there are none. Hosts often
    ///     package the same release differently (one file vs. split archives), so per-file mirrors cannot always cover a host
    ///     outage; these let the whole item move to another host. Planning them again picks the next host.
    /// </summary>
    public static String? FallbackLinks(String text, String? hostPreference = null, String? hostBlacklist = null)
    {
        var rest = RankHosts(text, hostPreference, hostBlacklist).Skip(1).SelectMany(g => g).ToList();

        return rest.Count > 0 ? String.Join('\n', rest) : null;
    }

    public static String HostOf(String link)
    {
        return HostKey(new Uri(link).Host);
    }

    /// <summary>
    ///     True for debrid errors about the hoster itself (AllDebrid LINK_HOST_UNAVAILABLE, LINK_HOST_NOT_SUPPORTED, …), where
    ///     retrying the same host is pointless.
    /// </summary>
    public static Boolean IsHostRejection(String? error)
    {
        return error != null && error.Contains("LINK_HOST_", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Attempts a direct download gets for a temporary error (<see cref="IsTemporaryError" />): with the runner's delays,
    ///     about 7 hours, enough to wait out a hoster's maintenance at the debrid service.
    /// </summary>
    public const Int32 TemporaryErrorRetryAttempts = 30;

    public static Int32 RetryAttempts(Int32 configured, Boolean temporary)
    {
        return temporary ? Math.Max(configured, TemporaryErrorRetryAttempts) : configured;
    }

    /// <summary>
    ///     True when the debrid service only refuses because too many downloads run on this host right now
    ///     (LINK_TOO_MANY_DOWNLOADS): waiting is better than moving the part to another host.
    /// </summary>
    public static Boolean IsHostBusy(String? error)
    {
        return error != null && error.Contains("LINK_TOO_MANY_DOWNLOADS", StringComparison.Ordinal);
    }

    private static readonly Regex TemporaryStatusRegex = new(@"status code does not indicate success: (429|502|503|504)\b", RegexOptions.Compiled);

    /// <summary>
    ///     True for errors that clear up by themselves: the hoster is under maintenance at the debrid service, too many
    ///     downloads run on it at once, or the debrid download server is briefly overloaded (HTTP 429/502/503/504). These are
    ///     worth waiting out for hours instead of failing the download after a few quick retries.
    /// </summary>
    public static Boolean IsTemporaryError(String? error)
    {
        return error != null &&
               (error.Contains("LINK_HOST_UNAVAILABLE", StringComparison.Ordinal) ||
                error.Contains("LINK_TOO_MANY_DOWNLOADS", StringComparison.Ordinal) ||
                error.Contains("LINK_TEMPORARY_UNAVAILABLE", StringComparison.Ordinal) ||
                TemporaryStatusRegex.IsMatch(error));
    }

    /// <summary>
    ///     Groups the links by host, best first: hosts with a complete archive set (no ".partN.rar" missing that another host
    ///     has), then the configured preference, then the number of links. Counting links alone would favour a host that
    ///     splits the release into archives over one that offers the whole file.
    /// </summary>
    private static List<List<String>> RankHosts(String text, String? hostPreference, String? hostBlacklist)
    {
        var preferred = HostPreference(hostPreference);

        var links = ExtractLinks(text);

        var blacklist = HostBlacklist(hostBlacklist);
        var allowed = links.Where(l => !blacklist.Contains(HostOf(l))).ToList();

        // A post that is only on blacklisted hosts is still tried, rather than failing for want of links.
        if (allowed.Count > 0)
        {
            links = allowed;
        }

        var lastPart = links.Select(RarPart)
                            .Where(p => p != null)
                            .Select(p => p!.Value)
                            .GroupBy(p => p.Base)
                            .ToDictionary(g => g.Key, g => g.Max(p => p.Number));

        return links.GroupBy(HostOf)
                    .OrderBy(g => HasEveryPart(g, lastPart) ? 0 : 1)
                    .ThenBy(g => PreferenceIndex(preferred, g.Key))
                    .ThenByDescending(g => g.Count())
                    .Select(g => g.ToList())
                    .ToList();
    }

    private static List<String> ExtractLinks(String text)
    {
        return UrlRegex().Matches(text)
                         .Select(m => m.Value.TrimEnd('.', ',', ')', ']', ';'))
                         .Where(m => Uri.TryCreate(m, UriKind.Absolute, out _))
                         .Distinct()
                         .ToList();
    }

    /// <summary>
    ///     Identifies an item added from pasted links rather than from a post: the same links pasted twice, in any order,
    ///     give the same hash.
    /// </summary>
    public static String LinksHash(String text)
    {
        return Hash("links:" + String.Join('\n', ExtractLinks(text).Order(StringComparer.Ordinal)));
    }

    /// <summary>
    ///     A release name for pasted links: the file name of the picked link, without ".html", its ".partN.rar" suffix or its
    ///     extension ("Movie.2026.1080p-GRP.part01.rar.html" → "Movie.2026.1080p-GRP"). Falls back to the host name.
    /// </summary>
    public static String ReleaseName(String link)
    {
        var name = HtmlSuffixRegex().Replace(HttpUtility.UrlDecode(new Uri(link).Segments.Last()).Trim('/'), "");

        if (!name.Contains('.'))
        {
            return HostOf(link);
        }

        var part = RarPartRegex().Match(name);

        name = part.Success ? name[..part.Index] : Path.GetFileNameWithoutExtension(name);

        return FileHelper.RemoveInvalidFileNameChars(name);
    }

    private static Boolean HasEveryPart(IEnumerable<String> hostLinks, Dictionary<String, Int32> lastPart)
    {
        return hostLinks.Select(RarPart)
                        .Where(p => p != null)
                        .Select(p => p!.Value)
                        .GroupBy(p => p.Base)
                        .All(g => Enumerable.Range(1, lastPart[g.Key]).All(n => g.Any(p => p.Number == n)));
    }

    private static (String Base, Int32 Number)? RarPart(String link)
    {
        var key = MirrorKey(link);

        if (key == null)
        {
            return null;
        }

        var match = RarPartRegex().Match(key);

        return match.Success ? (key[..match.Index], Int32.Parse(match.Groups[1].Value)) : null;
    }

    /// <summary>
    ///     The file name a hoster link points to, comparable across hosts: Rapidgator appends ".html", Nitroflare does not.
    /// </summary>
    public static String? MirrorKey(String link)
    {
        var name = HttpUtility.UrlDecode(new Uri(link).Segments.Last()).Trim('/');

        name = HtmlSuffixRegex().Replace(name, "");

        return name.Contains('.') ? name.ToLowerInvariant() : null;
    }

    public static Boolean IsSecondaryArchiveVolume(String? fileName)
    {
        if (String.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var part = RarPartRegex().Match(fileName);

        if (part.Success)
        {
            return Int32.Parse(part.Groups[1].Value) > 1;
        }

        return OldStyleVolumeRegex().IsMatch(fileName);
    }

    public static String FileNameFromLink(String link)
    {
        var name = HttpUtility.UrlDecode(new Uri(link).Segments.Last());

        return FileHelper.RemoveInvalidFileNameChars(name);
    }

    public static async Task NotifyWaiting(String? webhookUrl, String title, String postUrl)
    {
        if (String.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        var payload = new
        {
            content = $"**Links needed:** {title}\n{postUrl}"
        };

        using var response = await WebhookClient.PostAsJsonAsync(webhookUrl, payload);
        response.EnsureSuccessStatusCode();
    }

    private static String HostKey(String host)
    {
        var labels = host.ToLowerInvariant().Split('.', StringSplitOptions.RemoveEmptyEntries);
        var name = labels.Length >= 2 ? labels[^2] : host.ToLowerInvariant();

        return HostAliases.GetValueOrDefault(name, name);
    }

    /// <summary>
    ///     Resolves the host tie-break order: the UI setting, else the DDL_HOST_PREFERENCE environment variable, else the
    ///     default. Entries may be names, domains or aliases ("nitroflare", "rapidgator.net", "rg.to").
    /// </summary>
    /// <summary>
    ///     The blacklisted hosts (<see cref="HostBlacklistEnvVar" />), normalized like the preference. The value overrides
    ///     the environment variable (tests).
    /// </summary>
    public static ISet<String> HostBlacklist(String? value = null)
    {
        value ??= Environment.GetEnvironmentVariable(HostBlacklistEnvVar) ?? "";

        return value.Split([',', ';', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(HostKey)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static IList<String> HostPreference(String? setting)
    {
        var value = new[] { setting, Environment.GetEnvironmentVariable(HostPreferenceEnvVar) }.FirstOrDefault(v => !String.IsNullOrWhiteSpace(v))
                    ?? DefaultHostPreference;

        return value.Split([',', ';', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(HostKey)
                    .Distinct()
                    .ToList();
    }

    private static Int32 PreferenceIndex(IList<String> preferred, String hostKey)
    {
        var index = preferred.IndexOf(hostKey);

        return index < 0 ? preferred.Count : index;
    }
}
