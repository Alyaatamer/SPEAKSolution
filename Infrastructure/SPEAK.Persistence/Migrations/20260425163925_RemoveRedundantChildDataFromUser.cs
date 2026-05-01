using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPEAK.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantChildDataFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildAge",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChildBirthDate",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChildAge",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChildBirthDate",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }
    }
}
