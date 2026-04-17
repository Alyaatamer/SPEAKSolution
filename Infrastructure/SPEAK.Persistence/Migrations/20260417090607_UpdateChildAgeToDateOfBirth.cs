using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPEAK.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChildAgeToDateOfBirth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildAge",
                table: "ParentProfiles");

            migrationBuilder.AddColumn<DateTime>(
                name: "ChildBirthDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChildBirthDate",
                table: "ParentProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildBirthDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChildBirthDate",
                table: "ParentProfiles");

            migrationBuilder.AddColumn<int>(
                name: "ChildAge",
                table: "ParentProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
