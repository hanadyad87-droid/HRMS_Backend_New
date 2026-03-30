using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddQualificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "EmployeeEducations");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "EmployeeEducations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "QualificationId",
                table: "EmployeeEducations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Qualifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_QualificationId",
                table: "EmployeeEducations",
                column: "QualificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeEducations_Qualifications_QualificationId",
                table: "EmployeeEducations",
                column: "QualificationId",
                principalTable: "Qualifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeEducations_Qualifications_QualificationId",
                table: "EmployeeEducations");

            migrationBuilder.DropTable(
                name: "Qualifications");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeEducations_QualificationId",
                table: "EmployeeEducations");

            migrationBuilder.DropColumn(
                name: "QualificationId",
                table: "EmployeeEducations");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "EmployeeEducations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "EmployeeEducations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
