using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class ExitPermit_UpdateTimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PermitTime",
                table: "ExitPermitRequests",
                newName: "ToTime");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "FromTime",
                table: "ExitPermitRequests",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromTime",
                table: "ExitPermitRequests");

            migrationBuilder.RenameColumn(
                name: "ToTime",
                table: "ExitPermitRequests",
                newName: "PermitTime");
        }
    }
}
