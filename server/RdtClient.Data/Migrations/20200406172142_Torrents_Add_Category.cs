using Microsoft.EntityFrameworkCore.Migrations;

namespace RdtClient.Data.Migrations
{
    public partial class Torrents_Add_Category : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Torrents",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Torrents");
        }
    }
}
