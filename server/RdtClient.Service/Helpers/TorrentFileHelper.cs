using System.Security.Cryptography;
using MonoTorrent;
using MonoTorrent.BEncoding;

namespace RdtClient.Service.Helpers;

/// <summary>
///     What rdt-client reads from a .torrent file.
/// </summary>
public record TorrentFileInfo(String Name, String InfoHash, String? Source, IList<String> AnnounceUrls);

public static class TorrentFileHelper
{
    /// <summary>
    ///     Reads a .torrent file with MonoTorrent, or, when MonoTorrent rejects a file the debrid service can still use,
    ///     straight from its bencoded data. MonoTorrent refuses hybrid (BitTorrent v1 + v2) torrents whose v1 padding files
    ///     don't line up with the v2 file tree ("Inconsistent hybrid torrent, file padding mismatch"), although their v1
    ///     info hash is perfectly valid.
    /// </summary>
    public static async Task<TorrentFileInfo> Read(Byte[] bytes)
    {
        try
        {
            var torrent = await Torrent.LoadAsync(bytes);

            return new(torrent.Name, torrent.InfoHashes.V1OrV2.ToHex(), torrent.Source, torrent.AnnounceUrls?.SelectMany(m => m).ToList() ?? []);
        }
        catch (TorrentException)
        {
            return ReadRaw(bytes);
        }
    }

    /// <summary>
    ///     Reads the name, info hash and trackers without MonoTorrent's consistency checks. The v1 hash (SHA-1 of the info
    ///     dictionary) is the one MonoTorrent reports first for hybrid torrents, and the only one v1-only torrents have.
    /// </summary>
    public static TorrentFileInfo ReadRaw(Byte[] bytes)
    {
        var root = BEncodedValue.Decode<BEncodedDictionary>(bytes);

        if (!root.TryGetValue("info", out var infoValue) || infoValue is not BEncodedDictionary info)
        {
            throw new("Invalid torrent file: it has no info dictionary");
        }

        var infoBytes = info.Encode();
        var isV1 = info.ContainsKey("pieces");
        // Formatted by MonoTorrent's own InfoHash, so the hash reads exactly as it does for torrents MonoTorrent accepts.
        var hash = new InfoHash(isV1 ? SHA1.HashData(infoBytes) : SHA256.HashData(infoBytes)).ToHex();

        var name = info.TryGetValue("name", out var nameValue) && nameValue is BEncodedString nameString ? nameString.Text : hash;
        var source = info.TryGetValue("source", out var sourceValue) && sourceValue is BEncodedString sourceString ? sourceString.Text : null;

        var announceUrls = new List<String>();

        if (root.TryGetValue("announce", out var announceValue) && announceValue is BEncodedString announce)
        {
            announceUrls.Add(announce.Text);
        }

        if (root.TryGetValue("announce-list", out var listValue) && listValue is BEncodedList tiers)
        {
            announceUrls.AddRange(tiers.OfType<BEncodedList>().SelectMany(t => t.OfType<BEncodedString>()).Select(s => s.Text));
        }

        return new(name, hash, source, announceUrls.Distinct().ToList());
    }
}
