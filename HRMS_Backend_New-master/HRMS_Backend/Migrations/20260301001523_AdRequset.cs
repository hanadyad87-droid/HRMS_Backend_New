using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AdRequset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RequestSettings_TargetSubDepartmentId",
                table: "RequestSettings",
                column: "TargetSubDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestSettings_SubDepartments_TargetSubDepartmentId",
                table: "RequestSettings",
                column: "TargetSubDepartmentId",
                principalTable: "SubDepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestSettings_SubDepartments_TargetSubDepartmentId",
                table: "RequestSettings");

            migrationBuilder.DropIndex(
                name: "IX_RequestSettings_TargetSubDepartmentId",
                table: "RequestSettings");
        }
    }
}
