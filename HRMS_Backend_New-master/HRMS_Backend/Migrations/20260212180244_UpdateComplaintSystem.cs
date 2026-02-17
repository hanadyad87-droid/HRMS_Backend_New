using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateComplaintSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HandledByManagerId",
                table: "Complaints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsForAllDepartments",
                table: "Complaints",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HandledByManagerId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "IsForAllDepartments",
                table: "Complaints");
        }
    }
}
