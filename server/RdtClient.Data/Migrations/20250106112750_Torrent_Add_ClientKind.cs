using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdtClient.Data.Migrations
{
    /// <inheritdoc />
    public partial class Torrent_Add_ClientKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientKind",
                table: "Torrents",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientKind",
                table: "Torrents");
        }
    }
}
