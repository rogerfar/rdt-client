using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RdtClient.Data.Data;

#nullable disable

namespace RdtClient.Data.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20260423031719_Downloads_Add_TorrentIdPath_Unique")]
public partial class Downloads_Add_TorrentIdPath_Unique : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Orphaned downloads from previous failures.
        migrationBuilder.Sql("""
                             DELETE FROM Downloads
                             WHERE rowid NOT IN (
                                 SELECT MIN(rowid)
                                 FROM Downloads
                                 GROUP BY TorrentId, Path
                             );
                             """);

        migrationBuilder.DropIndex(
            name: "IX_Downloads_TorrentId",
            table: "Downloads");

        // Prevent accidental duplicates, provides idempotency at the DB level for downloads
        migrationBuilder.CreateIndex(
            name: "IX_Downloads_TorrentId_Path",
            table: "Downloads",
            columns: ["TorrentId", "Path"],
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Downloads_TorrentId_Path",
            table: "Downloads");

        migrationBuilder.CreateIndex(
            name: "IX_Downloads_TorrentId",
            table: "Downloads",
            column: "TorrentId");
    }
}
