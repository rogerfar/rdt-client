using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdtClient.Data.Migrations;

public partial class MoveSourcePayloadToPayloadTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TorrentPayloads",
            columns: table => new
            {
                TorrentId = table.Column<Guid>(type: "TEXT", nullable: false),
                Content = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TorrentPayloads", x => x.TorrentId);
                table.ForeignKey(
                    name: "FK_TorrentPayloads_Torrents_TorrentId",
                    column: x => x.TorrentId,
                    principalTable: "Torrents",
                    principalColumn: "TorrentId",
                    onDelete: ReferentialAction.NoAction);
            });

        migrationBuilder.Sql("""
                             INSERT INTO TorrentPayloads (TorrentId, Content)
                             SELECT TorrentId, FileOrMagnet
                             FROM Torrents
                             WHERE FileOrMagnet IS NOT NULL;
                             """);

        migrationBuilder.DropColumn(
            name: "FileOrMagnet",
            table: "Torrents");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FileOrMagnet",
            table: "Torrents",
            type: "TEXT",
            nullable: true);

        migrationBuilder.Sql("""
                             UPDATE Torrents
                             SET FileOrMagnet = (
                                 SELECT Content
                                 FROM TorrentPayloads
                                 WHERE TorrentPayloads.TorrentId = Torrents.TorrentId
                             )
                             WHERE TorrentId IN (
                                 SELECT TorrentId
                                 FROM TorrentPayloads
                             );
                             """);

        migrationBuilder.DropTable(
            name: "TorrentPayloads");
    }
}
