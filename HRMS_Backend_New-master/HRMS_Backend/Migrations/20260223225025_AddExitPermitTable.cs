using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddExitPermitTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExitPermitRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PermitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PermitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PermitTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsHrNotified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExitPermitRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExitPermitRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExitPermitRequests_EmployeeId",
                table: "ExitPermitRequests",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExitPermitRequests");
        }
    }
}
