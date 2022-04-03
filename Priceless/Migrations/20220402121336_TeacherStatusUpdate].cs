using Microsoft.EntityFrameworkCore.Migrations;

namespace Priceless.Migrations
{
    public partial class TeacherStatusUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MajorAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusComment",
                table: "MajorAssignments",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "MajorAssignments");

            migrationBuilder.DropColumn(
                name: "StatusComment",
                table: "MajorAssignments");
        }
    }
}
