using Microsoft.EntityFrameworkCore.Migrations;

namespace RdtClient.Data.Migrations
{
    public partial class Torrents_Removed_Auto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoDownload",
                table: "Torrents");

            migrationBuilder.DropColumn(
                name: "AutoUnpack",
                table: "Torrents");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoDownload",
                table: "Torrents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoUnpack",
                table: "Torrents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
