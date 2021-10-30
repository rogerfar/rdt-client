using Microsoft.EntityFrameworkCore.Migrations;

namespace RdtClient.Data.Migrations
{
    public partial class Torrents_Error : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "Torrents",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Error",
                table: "Torrents");
        }
    }
}
