using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixSubDepartmentSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManagerEmployeeId",
                table: "SubDepartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousManagerId",
                table: "SubDepartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagerEmployeeId",
                table: "Sections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousManagerId",
                table: "Sections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousManagerId",
                table: "Departments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubDepartments_DepartmentId",
                table: "SubDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubDepartments_ManagerEmployeeId",
                table: "SubDepartments",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubDepartments_PreviousManagerId",
                table: "SubDepartments",
                column: "PreviousManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_ManagerEmployeeId",
                table: "Sections",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_PreviousManagerId",
                table: "Sections",
                column: "PreviousManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_SubDepartmentId",
                table: "Sections",
                column: "SubDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_PreviousManagerId",
                table: "Departments",
                column: "PreviousManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_PreviousManagerId",
                table: "Departments",
                column: "PreviousManagerId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Employees_ManagerEmployeeId",
                table: "Sections",
                column: "ManagerEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Employees_PreviousManagerId",
                table: "Sections",
                column: "PreviousManagerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_SubDepartments_SubDepartmentId",
                table: "Sections",
                column: "SubDepartmentId",
                principalTable: "SubDepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubDepartments_Departments_DepartmentId",
                table: "SubDepartments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubDepartments_Employees_ManagerEmployeeId",
                table: "SubDepartments",
                column: "ManagerEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubDepartments_Employees_PreviousManagerId",
                table: "SubDepartments",
                column: "PreviousManagerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_PreviousManagerId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Employees_ManagerEmployeeId",
                table: "Sections");

            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Employees_PreviousManagerId",
                table: "Sections");

            migrationBuilder.DropForeignKey(
                name: "FK_Sections_SubDepartments_SubDepartmentId",
                table: "Sections");

            migrationBuilder.DropForeignKey(
                name: "FK_SubDepartments_Departments_DepartmentId",
                table: "SubDepartments");

            migrationBuilder.DropForeignKey(
                name: "FK_SubDepartments_Employees_ManagerEmployeeId",
                table: "SubDepartments");

            migrationBuilder.DropForeignKey(
                name: "FK_SubDepartments_Employees_PreviousManagerId",
                table: "SubDepartments");

            migrationBuilder.DropIndex(
                name: "IX_SubDepartments_DepartmentId",
                table: "SubDepartments");

            migrationBuilder.DropIndex(
                name: "IX_SubDepartments_ManagerEmployeeId",
                table: "SubDepartments");

            migrationBuilder.DropIndex(
                name: "IX_SubDepartments_PreviousManagerId",
                table: "SubDepartments");

            migrationBuilder.DropIndex(
                name: "IX_Sections_ManagerEmployeeId",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Sections_PreviousManagerId",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Sections_SubDepartmentId",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Departments_PreviousManagerId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "ManagerEmployeeId",
                table: "SubDepartments");

            migrationBuilder.DropColumn(
                name: "PreviousManagerId",
                table: "SubDepartments");

            migrationBuilder.DropColumn(
                name: "ManagerEmployeeId",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "PreviousManagerId",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "PreviousManagerId",
                table: "Departments");
        }
    }
}
