using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRMS_Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BankId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankBranches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FAQs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAQs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobGrades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    اسم_الاجازة = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    مخصومة_من_الرصيد = table.Column<bool>(type: "bit", nullable: false),
                    تحتاج_نموذج = table.Column<bool>(type: "bit", nullable: false),
                    تتأثر_بالعطلات_الرسمية = table.Column<bool>(type: "bit", nullable: false),
                    مفعلة = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaritalStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaritalStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfficialHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    IsTemporary = table.Column<bool>(type: "bit", nullable: false),
                    DelegationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetAll = table.Column<bool>(type: "bit", nullable: false),
                    TargetDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyForms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Complaints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    IsForAllDepartments = table.Column<bool>(type: "bit", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HandledByManagerId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complaints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataUpdateRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    UpdateType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataUpdateRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManagerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    PreviousManagerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotherName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaritalStatusId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeEducations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    QualificationId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Institution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeEducations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFinancialDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    BankId = table.Column<int>(type: "int", nullable: false),
                    BankBranchId = table.Column<int>(type: "int", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdministrativeNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JobGradeId = table.Column<int>(type: "int", nullable: false),
                    JobGradeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Allowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllowanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentLinkedSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentLinkedSalaryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DelegatedGradeId = table.Column<int>(type: "int", nullable: false),
                    DelegatedGradeDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeFinancialDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFinancialDatas_BankBranches_BankBranchId",
                        column: x => x.BankBranchId,
                        principalTable: "BankBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeFinancialDatas_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeFinancialDatas_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeFinancialDatas_JobGrades_DelegatedGradeId",
                        column: x => x.DelegatedGradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeFinancialDatas_JobGrades_JobGradeId",
                        column: x => x.JobGradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalDays = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    سبب_الرفض = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManagerNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EquipmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProblemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManagerDelegations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActingManagerId = table.Column<int>(type: "int", nullable: false),
                    OriginalManagerId = table.Column<int>(type: "int", nullable: false),
                    AssignedById = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagerDelegations_Employees_ActingManagerId",
                        column: x => x.ActingManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerDelegations_Employees_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerDelegations_Employees_OriginalManagerId",
                        column: x => x.OriginalManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryCertificateRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryCertificateRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryCertificateRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    ManagerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    PreviousManagerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubDepartments_Employees_ManagerEmployeeId",
                        column: x => x.ManagerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubDepartments_Employees_PreviousManagerId",
                        column: x => x.PreviousManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    TargetSubDepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestSettings_SubDepartments_TargetSubDepartmentId",
                        column: x => x.TargetSubDepartmentId,
                        principalTable: "SubDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubDepartmentId = table.Column<int>(type: "int", nullable: false),
                    ManagerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    PreviousManagerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Employees_ManagerEmployeeId",
                        column: x => x.ManagerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sections_Employees_PreviousManagerId",
                        column: x => x.PreviousManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sections_SubDepartments_SubDepartmentId",
                        column: x => x.SubDepartmentId,
                        principalTable: "SubDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAdministrativeDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    JobStatus = table.Column<int>(type: "int", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    SubDepartmentId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    StartWorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkLocationId = table.Column<int>(type: "int", nullable: false),
                    JobGradeId = table.Column<int>(type: "int", nullable: false),
                    LeaveBalance = table.Column<double>(type: "float", nullable: false),
                    ContractStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransferType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransferFromEntityId = table.Column<int>(type: "int", nullable: true),
                    TransferStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransferEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecondmentToEntityId = table.Column<int>(type: "int", nullable: true),
                    SecondmentStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecondmentEndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAdministrativeDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_Employees_SecondmentToEntityId",
                        column: x => x.SecondmentToEntityId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_Employees_TransferFromEntityId",
                        column: x => x.TransferFromEntityId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_JobGrades_JobGradeId",
                        column: x => x.JobGradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_SubDepartments_SubDepartmentId",
                        column: x => x.SubDepartmentId,
                        principalTable: "SubDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAdministrativeDatas_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AssignedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeAttachment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManagerDecision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_Employees_AssignedByEmployeeId",
                        column: x => x.AssignedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskAssignmentId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComments_TaskAssignments_TaskAssignmentId",
                        column: x => x.TaskAssignmentId,
                        principalTable: "TaskAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "JobTitles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "موظف" },
                    { 2, "فني" },
                    { 3, "مدير" },
                    { 4, "مأمور ضبظ" },
                    { 5, "مأمور ضبظ قضائي" },
                    { 6, "عضو تحقيق" }
                });

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "تتأثر_بالعطلات_الرسمية", "اسم_الاجازة", "تحتاج_نموذج", "مخصومة_من_الرصيد", "مفعلة" },
                values: new object[,]
                {
                    { 1, true, "إجازة سنوية", false, true, true },
                    { 2, true, "إجازة مرضية", true, true, true },
                    { 3, false, "إجازة حج", true, false, true },
                    { 4, false, "إجازة عمرة", true, false, true },
                    { 5, false, "إجازة وضع", true, false, true },
                    { 6, true, "إجازة طارئه", false, true, true }
                });

            migrationBuilder.InsertData(
                table: "MaritalStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "أعزب" },
                    { 2, "متزوج" },
                    { 3, "مطلق" },
                    { 4, "أرمل" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "PermissionName" },
                values: new object[,]
                {
                    { 1, "AddEmployee" },
                    { 2, "EditEmployee" },
                    { 3, "DeleteEmployee" },
                    { 4, "ViewEmployee" },
                    { 5, "ApproveLeave" },
                    { 6, "SubmitLeave" },
                    { 7, "SubmitComplaint" },
                    { 8, "ViewComplaints" },
                    { 9, "AssignTask" },
                    { 10, "ViewDepartmentEmployees" },
                    { 11, "AddOwnEducation" },
                    { 12, "EditOwnEducation" },
                    { 13, "ManageEmployeeEducation" },
                    { 14, "AssignRole" },
                    { 15, "ManageMaintenance" },
                    { 16, "ManageSalaryCertificates" },
                    { 17, "ManageExitPermits" },
                    { 18, "ManageDataUpdates" },
                    { 19, "ManageForms" },
                    { 20, "ViewReports" },
                    { 21, "AddEmployeeEducation" },
                    { 22, "EditEmployeeEducation" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "RoleName" },
                values: new object[,]
                {
                    { 1, "SuperAdmin" },
                    { 2, "HR" },
                    { 3, "DepartmentManager" },
                    { 4, "SubDepartmentManager" },
                    { 5, "SectionManager" },
                    { 6, "Employee" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 },
                    { 9, 1 },
                    { 10, 1 },
                    { 11, 1 },
                    { 12, 1 },
                    { 13, 1 },
                    { 14, 1 },
                    { 15, 1 },
                    { 16, 1 },
                    { 17, 1 },
                    { 18, 1 },
                    { 20, 1 },
                    { 21, 1 },
                    { 22, 1 },
                    { 1, 2 },
                    { 2, 2 },
                    { 4, 2 },
                    { 17, 2 },
                    { 18, 2 },
                    { 5, 3 },
                    { 10, 3 },
                    { 20, 3 },
                    { 5, 4 },
                    { 10, 4 },
                    { 19, 4 },
                    { 20, 4 },
                    { 5, 5 },
                    { 10, 5 },
                    { 17, 5 },
                    { 20, 5 },
                    { 4, 6 },
                    { 6, 6 },
                    { 11, 6 },
                    { 12, 6 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_TargetDepartmentId",
                table: "Announcements",
                column: "TargetDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyForms_UploadedByEmployeeId",
                table: "CompanyForms",
                column: "UploadedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_DepartmentId",
                table: "Complaints",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_EmployeeId",
                table: "Complaints",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DataUpdateRequests_EmployeeId",
                table: "DataUpdateRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerEmployeeId",
                table: "Departments",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_PreviousManagerId",
                table: "Departments",
                column: "PreviousManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_DepartmentId",
                table: "EmployeeAdministrativeDatas",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_EmployeeId",
                table: "EmployeeAdministrativeDatas",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_JobGradeId",
                table: "EmployeeAdministrativeDatas",
                column: "JobGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_JobTitleId",
                table: "EmployeeAdministrativeDatas",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_SecondmentToEntityId",
                table: "EmployeeAdministrativeDatas",
                column: "SecondmentToEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_SectionId",
                table: "EmployeeAdministrativeDatas",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_SubDepartmentId",
                table: "EmployeeAdministrativeDatas",
                column: "SubDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_TransferFromEntityId",
                table: "EmployeeAdministrativeDatas",
                column: "TransferFromEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAdministrativeDatas_WorkLocationId",
                table: "EmployeeAdministrativeDatas",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_EmployeeId",
                table: "EmployeeEducations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_QualificationId",
                table: "EmployeeEducations",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFinancialDatas_BankBranchId",
                table: "EmployeeFinancialDatas",
                column: "BankBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFinancialDatas_BankId",
                table: "EmployeeFinancialDatas",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFinancialDatas_DelegatedGradeId",
                table: "EmployeeFinancialDatas",
                column: "DelegatedGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFinancialDatas_EmployeeId",
                table: "EmployeeFinancialDatas",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFinancialDatas_JobGradeId",
                table: "EmployeeFinancialDatas",
                column: "JobGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeId",
                table: "Employees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_MaritalStatusId",
                table: "Employees",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PublicId",
                table: "Employees",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExitPermitRequests_EmployeeId",
                table: "ExitPermitRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_EmployeeId",
                table: "MaintenanceRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDelegations_ActingManagerId",
                table: "ManagerDelegations",
                column: "ActingManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDelegations_AssignedById",
                table: "ManagerDelegations",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDelegations_OriginalManagerId",
                table: "ManagerDelegations",
                column: "OriginalManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSettings_TargetSubDepartmentId",
                table: "RequestSettings",
                column: "TargetSubDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryCertificateRequests_EmployeeId",
                table: "SalaryCertificateRequests",
                column: "EmployeeId");

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
                name: "IX_TaskAssignments_AssignedByEmployeeId",
                table: "TaskAssignments",
                column: "AssignedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_EmployeeId",
                table: "TaskAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_SectionId",
                table: "TaskAssignments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskAssignmentId",
                table: "TaskComments",
                column: "TaskAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Departments_TargetDepartmentId",
                table: "Announcements",
                column: "TargetDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyForms_Employees_UploadedByEmployeeId",
                table: "CompanyForms",
                column: "UploadedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Departments_DepartmentId",
                table: "Complaints",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Employees_EmployeeId",
                table: "Complaints",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DataUpdateRequests_Employees_EmployeeId",
                table: "DataUpdateRequests",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_ManagerEmployeeId",
                table: "Departments",
                column: "ManagerEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_PreviousManagerId",
                table: "Departments",
                column: "PreviousManagerId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Departments_DepartmentId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CompanyForms");

            migrationBuilder.DropTable(
                name: "Complaints");

            migrationBuilder.DropTable(
                name: "DataUpdateRequests");

            migrationBuilder.DropTable(
                name: "EmployeeAdministrativeDatas");

            migrationBuilder.DropTable(
                name: "EmployeeEducations");

            migrationBuilder.DropTable(
                name: "EmployeeFinancialDatas");

            migrationBuilder.DropTable(
                name: "EmploymentStatuses");

            migrationBuilder.DropTable(
                name: "ExitPermitRequests");

            migrationBuilder.DropTable(
                name: "FAQs");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "MaintenanceRequests");

            migrationBuilder.DropTable(
                name: "ManagerDelegations");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OfficialHolidays");

            migrationBuilder.DropTable(
                name: "RequestSettings");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SalaryCertificateRequests");

            migrationBuilder.DropTable(
                name: "TaskComments");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "JobTitles");

            migrationBuilder.DropTable(
                name: "WorkLocations");

            migrationBuilder.DropTable(
                name: "Qualifications");

            migrationBuilder.DropTable(
                name: "BankBranches");

            migrationBuilder.DropTable(
                name: "Banks");

            migrationBuilder.DropTable(
                name: "JobGrades");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropTable(
                name: "TaskAssignments");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "SubDepartments");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "MaritalStatuses");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
