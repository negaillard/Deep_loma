using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddSequentialSigning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "DocumentUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSequential",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "DocumentUsers");

            migrationBuilder.DropColumn(
                name: "IsSequential",
                table: "Documents");
        }
    }
}
