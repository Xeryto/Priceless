using Microsoft.EntityFrameworkCore.Migrations;

namespace Priceless.Migrations
{
    public partial class StatusUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Admissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusComment",
                table: "Admissions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "StatusComment",
                table: "Admissions");
        }
    }
}
