using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RdtClient.Data.Models.Data;

public class Download
{
    [Key]
    public Guid DownloadId { get; set; }

    public Guid TorrentId { get; set; }

    [ForeignKey("TorrentId")]
    public Torrent? Torrent { get; set; }

    public String Path { get; set; } = null!;
    public String? Link { get; set; }

    public DateTimeOffset Added { get; set; }
    public DateTimeOffset? DownloadQueued { get; set; }
    public DateTimeOffset? DownloadStarted { get; set; }
    public DateTimeOffset? DownloadFinished { get; set; }
    public DateTimeOffset? UnpackingQueued { get; set; }
    public DateTimeOffset? UnpackingStarted { get; set; }
    public DateTimeOffset? UnpackingFinished { get; set; }
    public DateTimeOffset? Completed { get; set; }

    public Int32 RetryCount { get; set; }

    public String? Error { get; set; }

    public String? RemoteId { get; set; }

    public String? FileName { get; set; }

    [NotMapped]
    public Int64 BytesTotal { get; set; }

    [NotMapped]
    public Int64 BytesDone { get; set; }

    [NotMapped]
    public Int64 Speed { get; set; }
}

/// <summary>
/// Used to create <see cref="Download"/>s
/// </summary>
public class DownloadInfo
{
    /// <summary>
    /// The name of the file. Should not include directory.
    /// If the filename is not known, set tn null and `GetFileName` will be called with the unrestricted link.
    /// </summary>
    public required String? FileName;
    /// <summary>
    /// The restricted link to download this download. If the debrid serice in question does not have restricted links, use either a fake or the unrestricted link
    /// </summary>
    public required String RestrictedLink;
}