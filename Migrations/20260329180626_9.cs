using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VacationManager.Migrations
{
    /// <inheritdoc />
    public partial class _9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "VacationRequests");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "VacationRequests",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "VacationRequests");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "VacationRequests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
