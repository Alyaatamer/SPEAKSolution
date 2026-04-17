using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPEAK.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvatarId",
                table: "ParentProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarId",
                table: "ParentProfiles");
        }
    }
}
