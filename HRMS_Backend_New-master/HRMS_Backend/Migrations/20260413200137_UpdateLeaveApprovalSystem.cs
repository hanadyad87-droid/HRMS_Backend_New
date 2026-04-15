using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeaveApprovalSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "LeaveRequests");

            migrationBuilder.RenameColumn(
                name: "ManagerNote",
                table: "LeaveRequests",
                newName: "PartialNote");

            migrationBuilder.AddColumn<bool>(
                name: "FinalApproval",
                table: "LeaveRequests",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalNote",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PartialApproval",
                table: "LeaveRequests",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalApproval",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "FinalNote",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "PartialApproval",
                table: "LeaveRequests");

            migrationBuilder.RenameColumn(
                name: "PartialNote",
                table: "LeaveRequests",
                newName: "ManagerNote");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "LeaveRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
