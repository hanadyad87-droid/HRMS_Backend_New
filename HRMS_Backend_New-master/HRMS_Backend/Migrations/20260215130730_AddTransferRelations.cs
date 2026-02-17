using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "LeaveBalance",
                table: "EmployeeAdministrativeDatas",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_SecondmentToEntityId",
                table: "EmployeeAdministrativeDatas",
                column: "SecondmentToEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_TransferFromEntityId",
                table: "EmployeeAdministrativeDatas",
                column: "TransferFromEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Employees_SecondmentToEntityId",
                table: "EmployeeAdministrativeDatas",
                column: "SecondmentToEntityId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Employees_TransferFromEntityId",
                table: "EmployeeAdministrativeDatas",
                column: "TransferFromEntityId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Employees_SecondmentToEntityId",
                table: "EmployeeAdministrativeDatas");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Employees_TransferFromEntityId",
                table: "EmployeeAdministrativeDatas");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAdministrativeDatas_SecondmentToEntityId",
                table: "EmployeeAdministrativeDatas");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAdministrativeDatas_TransferFromEntityId",
                table: "EmployeeAdministrativeDatas");

            migrationBuilder.AlterColumn<int>(
                name: "LeaveBalance",
                table: "EmployeeAdministrativeDatas",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
