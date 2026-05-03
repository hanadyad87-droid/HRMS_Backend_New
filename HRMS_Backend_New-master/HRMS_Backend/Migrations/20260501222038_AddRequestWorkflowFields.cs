using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataUpdateRequests_Employees_EmployeeId",
                table: "DataUpdateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_Employees_EmployeeId",
                table: "MaintenanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryCertificateRequests_Employees_EmployeeId",
                table: "SalaryCertificateRequests");

            migrationBuilder.AddColumn<int>(
                name: "AssignedToEmployeeId",
                table: "SalaryCertificateRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "SalaryCertificateRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClaimedByEmployeeId",
                table: "SalaryCertificateRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "SalaryCertificateRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "SalaryCertificateRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "SalaryCertificateRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedToEmployeeId",
                table: "MaintenanceRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "MaintenanceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClaimedByEmployeeId",
                table: "MaintenanceRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "MaintenanceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "MaintenanceRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "MaintenanceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedToEmployeeId",
                table: "DataUpdateRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "DataUpdateRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClaimedByEmployeeId",
                table: "DataUpdateRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "DataUpdateRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "DataUpdateRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "DataUpdateRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryCertificateRequests_AssignedToEmployeeId",
                table: "SalaryCertificateRequests",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryCertificateRequests_ClaimedByEmployeeId",
                table: "SalaryCertificateRequests",
                column: "ClaimedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_AssignedToEmployeeId",
                table: "MaintenanceRequests",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_ClaimedByEmployeeId",
                table: "MaintenanceRequests",
                column: "ClaimedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DataUpdateRequests_AssignedToEmployeeId",
                table: "DataUpdateRequests",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DataUpdateRequests_ClaimedByEmployeeId",
                table: "DataUpdateRequests",
                column: "ClaimedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_DataUpdateRequests_Employees_AssignedToEmployeeId",
                table: "DataUpdateRequests",
                column: "AssignedToEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DataUpdateRequests_Employees_ClaimedByEmployeeId",
                table: "DataUpdateRequests",
                column: "ClaimedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DataUpdateRequests_Employees_EmployeeId",
                table: "DataUpdateRequests",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_Employees_AssignedToEmployeeId",
                table: "MaintenanceRequests",
                column: "AssignedToEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_Employees_ClaimedByEmployeeId",
                table: "MaintenanceRequests",
                column: "ClaimedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_Employees_EmployeeId",
                table: "MaintenanceRequests",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryCertificateRequests_Employees_AssignedToEmployeeId",
                table: "SalaryCertificateRequests",
                column: "AssignedToEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryCertificateRequests_Employees_ClaimedByEmployeeId",
                table: "SalaryCertificateRequests",
                column: "ClaimedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryCertificateRequests_Employees_EmployeeId",
                table: "SalaryCertificateRequests",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataUpdateRequests_Employees_AssignedToEmployeeId",
                table: "DataUpdateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DataUpdateRequests_Employees_ClaimedByEmployeeId",
                table: "DataUpdateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DataUpdateRequests_Employees_EmployeeId",
                table: "DataUpdateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_Employees_AssignedToEmployeeId",
                table: "MaintenanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_Employees_ClaimedByEmployeeId",
                table: "MaintenanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_Employees_EmployeeId",
                table: "MaintenanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryCertificateRequests_Employees_AssignedToEmployeeId",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryCertificateRequests_Employees_ClaimedByEmployeeId",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryCertificateRequests_Employees_EmployeeId",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalaryCertificateRequests_AssignedToEmployeeId",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropIndex(
                name: "IX_SalaryCertificateRequests_ClaimedByEmployeeId",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_AssignedToEmployeeId",
                table: "MaintenanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_ClaimedByEmployeeId",
                table: "MaintenanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_DataUpdateRequests_AssignedToEmployeeId",
                table: "DataUpdateRequests");

            migrationBuilder.DropIndex(
                name: "IX_DataUpdateRequests_ClaimedByEmployeeId",
                table: "DataUpdateRequests");

            migrationBuilder.DropColumn(
                name: "AssignedToEmployeeId",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropColumn(
                name: "ClaimedByEmployeeId",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "SalaryCertificateRequests");

            migrationBuilder.DropColumn(
                name: "AssignedToEmployeeId",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "ClaimedByEmployeeId",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "AssignedToEmployeeId",
                table: "DataUpdateRequests");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "DataUpdateRequests");

            migrationBuilder.DropColumn(
                name: "ClaimedByEmployeeId",
                table: "DataUpdateRequests");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "DataUpdateRequests");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "DataUpdateRequests");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "DataUpdateRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_DataUpdateRequests_Employees_EmployeeId",
                table: "DataUpdateRequests",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_Employees_EmployeeId",
                table: "MaintenanceRequests",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryCertificateRequests_Employees_EmployeeId",
                table: "SalaryCertificateRequests",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
