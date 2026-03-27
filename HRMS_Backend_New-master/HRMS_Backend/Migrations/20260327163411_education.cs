using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class education : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GraduationYear",
                table: "EmployeeEducations");

            migrationBuilder.RenameColumn(
                name: "University",
                table: "EmployeeEducations",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "Major",
                table: "EmployeeEducations",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Degree",
                table: "EmployeeEducations",
                newName: "Institution");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "PermissionName" },
                values: new object[,]
                {
                    { 21, "AddEmployeeEducation" },
                    { 22, "EditEmployeeEducation" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 21, 1 },
                    { 22, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 21, 1 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 22, 1 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "EmployeeEducations",
                newName: "University");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "EmployeeEducations",
                newName: "Major");

            migrationBuilder.RenameColumn(
                name: "Institution",
                table: "EmployeeEducations",
                newName: "Degree");

            migrationBuilder.AddColumn<int>(
                name: "GraduationYear",
                table: "EmployeeEducations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
