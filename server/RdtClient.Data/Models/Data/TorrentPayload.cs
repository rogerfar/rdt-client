using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RdtClient.Data.Models.Data;

public class TorrentPayload
{
    [Key]
    [ForeignKey(nameof(Torrent))]
    public Guid TorrentId { get; set; }

    public String Content { get; set; } = null!;

    public Torrent Torrent { get; set; } = null!;
}
