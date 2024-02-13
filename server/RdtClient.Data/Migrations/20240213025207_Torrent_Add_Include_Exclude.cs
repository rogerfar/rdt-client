using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RdtClient.Data.Migrations
{
    /// <inheritdoc />
    public partial class Torrent_Add_Include_Exclude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExcludeRegex",
                table: "Torrents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncludeRegex",
                table: "Torrents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludeRegex",
                table: "Torrents");

            migrationBuilder.DropColumn(
                name: "IncludeRegex",
                table: "Torrents");
        }
    }
}
