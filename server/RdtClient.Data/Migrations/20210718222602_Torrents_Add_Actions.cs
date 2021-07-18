using Microsoft.EntityFrameworkCore.Migrations;

namespace RdtClient.Data.Migrations
{
    public partial class Torrents_Add_Actions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoDelete",
                table: "Torrents");

            migrationBuilder.AddColumn<int>(
                name: "FinishedAction",
                table: "Torrents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DownloadAction",
                table: "Torrents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DownloadManualFiles",
                table: "Torrents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DownloadMinSize",
                table: "Torrents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadAction",
                table: "Torrents");

            migrationBuilder.DropColumn(
                name: "DownloadManualFiles",
                table: "Torrents");

            migrationBuilder.DropColumn(
                name: "DownloadMinSize",
                table: "Torrents");

            migrationBuilder.DropColumn(
                name: "FinishedAction",
                table: "Torrents");

            migrationBuilder.AddColumn<int>(
                name: "AutoDelete",
                table: "Torrents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
