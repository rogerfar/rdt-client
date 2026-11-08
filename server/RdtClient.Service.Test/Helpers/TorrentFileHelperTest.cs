using System.Security.Cryptography;
using MonoTorrent;
using MonoTorrent.BEncoding;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class TorrentFileHelperTest
{
    private static BEncodedDictionary File(Int64 length, String name)
    {
        return new()
        {
            ["length"] = new BEncodedNumber(length),
            ["path"] = new BEncodedList { new BEncodedString(name) }
        };
    }

    private static BEncodedDictionary Info(Boolean hybrid)
    {
        var info = new BEncodedDictionary
        {
            ["name"] = new BEncodedString("Movie.1982.2160p.WEB-GRP"),
            ["piece length"] = new BEncodedNumber(16384),
            ["pieces"] = new BEncodedString(new Byte[20]),
            ["files"] = new BEncodedList { File(10, "movie.mkv"), File(5, "movie.nfo") }
        };

        if (hybrid)
        {
            // v2 metadata for the same two files, but the v1 list above lacks the padding file a hybrid needs after
            // movie.mkv, which is what MonoTorrent rejects.
            BEncodedDictionary Leaf(Int64 length)
            {
                return new() { [""] = new BEncodedDictionary { ["length"] = new BEncodedNumber(length), ["pieces root"] = new BEncodedString(new Byte[32]) } };
            }

            info["meta version"] = new BEncodedNumber(2);
            info["file tree"] = new BEncodedDictionary { ["movie.mkv"] = Leaf(10), ["movie.nfo"] = Leaf(5) };
        }

        return info;
    }

    private static Byte[] TorrentFile(BEncodedDictionary info)
    {
        return new BEncodedDictionary
        {
            ["announce"] = new BEncodedString("udp://tracker.example:1337/announce"),
            ["announce-list"] = new BEncodedList
            {
                new BEncodedList { new BEncodedString("udp://tracker.example:1337/announce") },
                new BEncodedList { new BEncodedString("https://other.example/announce") }
            },
            ["info"] = info
        }.Encode();
    }

    [Fact]
    public async Task ReadRaw_MatchesMonoTorrentOnAPlainTorrent()
    {
        var bytes = TorrentFile(Info(false));

        var mono = await Torrent.LoadAsync(bytes);
        var raw = TorrentFileHelper.ReadRaw(bytes);

        Assert.Equal(mono.InfoHashes.V1OrV2.ToHex(), raw.InfoHash);
        Assert.Equal(mono.Name, raw.Name);
        Assert.Equal(["udp://tracker.example:1337/announce", "https://other.example/announce"], raw.AnnounceUrls);
    }

    [Fact]
    public async Task Read_AcceptsAHybridTorrentMonoTorrentRejects()
    {
        var info = Info(true);
        var bytes = TorrentFile(info);

        var rejected = await Assert.ThrowsAsync<TorrentException>(() => Torrent.LoadAsync(bytes));
        Assert.Contains("hybrid", rejected.Message, StringComparison.OrdinalIgnoreCase);

        var read = await TorrentFileHelper.Read(bytes);

        Assert.Equal(Convert.ToHexString(SHA1.HashData(info.Encode())), read.InfoHash, ignoreCase: true);
        Assert.Equal("Movie.1982.2160p.WEB-GRP", read.Name);
    }
}
