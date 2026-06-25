using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RdtClient.Data.Models.Data;

namespace RdtClient.Data.Data;

#nullable disable

public class DataContext(DbContextOptions options) : IdentityDbContext(options)
{
    public DbSet<Download> Downloads { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<Torrent> Torrents { get; set; }
    public DbSet<TorrentPayload> TorrentPayloads { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Download>()
               .HasIndex(m => new
               {
                   m.TorrentId,
                   m.Path
               })
               .IsUnique();

        builder.Entity<Torrent>()
               .HasOne(m => m.Payload)
               .WithOne(m => m.Torrent)
               .HasForeignKey<TorrentPayload>(m => m.TorrentId)
               .IsRequired(false);

        var cascadeFKs = builder.Model.GetEntityTypes()
                                .SelectMany(t => t.GetForeignKeys())
                                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

        foreach (var fk in cascadeFKs)
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
