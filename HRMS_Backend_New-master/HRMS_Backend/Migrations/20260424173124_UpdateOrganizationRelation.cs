using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrganizationRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Employees_SecondmentToEntityId",
                table: "EmployeeAdministrativeDatas");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Employees_TransferFromEntityId",
                table: "EmployeeAdministrativeDatas");

            migrationBuilder.RenameColumn(
                name: "TransferFromEntityId",
                table: "EmployeeAdministrativeDatas",
                newName: "TransferFromOrganizationId");

            migrationBuilder.RenameColumn(
                name: "SecondmentToEntityId",
                table: "EmployeeAdministrativeDatas",
                newName: "SecondmentToOrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeAdministrativeDatas_TransferFromEntityId",
                table: "EmployeeAdministrativeDatas",
                newName: "IX_EmployeeAdministrativeDatas_TransferFromOrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeAdministrativeDatas_SecondmentToEntityId",
                table: "EmployeeAdministrativeDatas",
                newName: "IX_EmployeeAdministrativeDatas_SecondmentToOrganizationId");

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Organizations_SecondmentToOrganizationId",
                table: "EmployeeAdministrativeDatas",
                column: "SecondmentToOrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Organizations_TransferFromOrganizationId",
                table: "EmployeeAdministrativeDatas",
                column: "TransferFromOrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Organizations_SecondmentToOrganizationId",
                table: "EmployeeAdministrativeDatas");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAdministrativeDatas_Organizations_TransferFromOrganizationId",
                table: "EmployeeAdministrativeDatas");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.RenameColumn(
                name: "TransferFromOrganizationId",
                table: "EmployeeAdministrativeDatas",
                newName: "TransferFromEntityId");

            migrationBuilder.RenameColumn(
                name: "SecondmentToOrganizationId",
                table: "EmployeeAdministrativeDatas",
                newName: "SecondmentToEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeAdministrativeDatas_TransferFromOrganizationId",
                table: "EmployeeAdministrativeDatas",
                newName: "IX_EmployeeAdministrativeDatas_TransferFromEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeAdministrativeDatas_SecondmentToOrganizationId",
                table: "EmployeeAdministrativeDatas",
                newName: "IX_EmployeeAdministrativeDatas_SecondmentToEntityId");

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
    }
}
