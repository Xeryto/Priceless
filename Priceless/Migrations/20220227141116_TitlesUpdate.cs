using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Priceless.Migrations
{
    public partial class TitlesUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "File",
                table: "Uploads");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Uploads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "Uploads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Uploads");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "Uploads");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Exercises");

            migrationBuilder.AddColumn<byte[]>(
                name: "File",
                table: "Uploads",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
