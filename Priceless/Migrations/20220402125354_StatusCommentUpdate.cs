using Microsoft.EntityFrameworkCore.Migrations;

namespace Priceless.Migrations
{
    public partial class StatusCommentUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusComment",
                table: "People");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatusComment",
                table: "People",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
