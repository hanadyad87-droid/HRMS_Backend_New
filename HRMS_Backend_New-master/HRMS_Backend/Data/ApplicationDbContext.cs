using HRMS_Backend.Enums;
using HRMS_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS_Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        public DbSet<MaritalStatus> MaritalStatuses { get; set; } = null!;
        public DbSet<JobTitle> JobTitles { get; set; } = null!;
        public DbSet<EmploymentStatus> EmploymentStatuses { get; set; } = null!;
        public DbSet<JobGrade> JobGrades { get; set; } = null!;
        public DbSet<WorkLocation> WorkLocations { get; set; } = null!;

        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<subDepartment> SubDepartments { get; set; } = null!;
        public DbSet<Section> Sections { get; set; } = null!;

        public DbSet<Bank> Banks { get; set; } = null!;
        public DbSet<BankBranch> BankBranches { get; set; } = null!;
        public DbSet<EmployeeFinancialData> EmployeeFinancialDatas { get; set; } = null!;
        public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
        public DbSet<LeaveTypes> LeaveTypes { get; set; } = null!;

        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<OfficialHoliday> OfficialHolidays { get; set; } = null!;
        public DbSet<EmployeeEducation> EmployeeEducations { get; set; } = null!;
        public DbSet<EmployeeAdministrativeData> EmployeeAdministrativeDatas { get; set; } = null!;
        public DbSet<UserPermission> UserPermissions { get; set; } = null!;
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Complaint> Complaints { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔐 Employee PublicId Unique
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.PublicId)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<Employee>()
    .HasIndex(e => e.EmployeeNumber)
    .IsUnique();


            modelBuilder.Entity<UserPermission>()
        .HasKey(up => new { up.UserId, up.PermissionId });

            // UserRole → مفتاح مركب
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            // RolePermission → مفتاح مركب
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Department → ManagerEmployee
            modelBuilder.Entity<Department>()
                .HasOne(d => d.ManagerEmployee)
                .WithMany()
                .HasForeignKey(d => d.ManagerEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // EmployeeEducation → Employee
            modelBuilder.Entity<EmployeeEducation>()
                .HasOne(e => e.Employee)
                .WithMany(e => e.Educations)
                .HasForeignKey(e => e.EmployeeId);

            // EmployeeAdministrativeData → Employee (One-to-One)
            modelBuilder.Entity<EmployeeAdministrativeData>()
                .HasOne(a => a.Employee)
                .WithOne(e => e.AdministrativeData)
                .HasForeignKey<EmployeeAdministrativeData>(a => a.EmployeeId);
            // ================= Transfer & Secondment (Self Reference) =================

            modelBuilder.Entity<EmployeeAdministrativeData>()
                .HasOne(a => a.TransferFromEntity)
                .WithMany()
                .HasForeignKey(a => a.TransferFromEntityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeAdministrativeData>()
                .HasOne(a => a.SecondmentToEntity)
                .WithMany()
                .HasForeignKey(a => a.SecondmentToEntityId)
                .OnDelete(DeleteBehavior.Restrict);

            // LeaveRequest → Status default
            modelBuilder.Entity<LeaveRequest>()
                .Property(l => l.Status)
                .HasDefaultValue(LeaveStatus.قيد_الانتظار);
            modelBuilder.Entity<EmployeeFinancialData>()
      .HasOne(e => e.JobGrade)
      .WithMany()
      .HasForeignKey(e => e.JobGradeId)
      .OnDelete(DeleteBehavior.NoAction); // <--- بدل Cascade

            modelBuilder.Entity<EmployeeFinancialData>()
                .HasOne(e => e.DelegatedGrade)
                .WithMany()
                .HasForeignKey(e => e.DelegatedGradeId)
                .OnDelete(DeleteBehavior.NoAction); // <--- بدل Cascade

            //--------ريماز------//

            modelBuilder.Entity<subDepartment>(entity =>
            {
                // علاقتها بالإدارة الأب
                entity.HasOne(s => s.Department)
                      .WithMany(d => d.SubDepartments)
                      .HasForeignKey(s => s.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // علاقتها بالمدراء
                entity.HasOne(s => s.ManagerEmployee)
                      .WithMany()
                      .HasForeignKey(s => s.ManagerEmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.PreviousManager)
                      .WithMany()
                      .HasForeignKey(s => s.PreviousManagerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Section>(entity =>
            {
                // علاقتها بالإدارة الفرعية الأب
                entity.HasOne(sec => sec.SubDepartment)
                      .WithMany(s => s.Sections)
                      .HasForeignKey(sec => sec.SubDepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // علاقتها بالمدراء
                entity.HasOne(sec => sec.ManagerEmployee)
                      .WithMany()
                      .HasForeignKey(sec => sec.ManagerEmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sec => sec.PreviousManager)
                      .WithMany()
                      .HasForeignKey(sec => sec.PreviousManagerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Complaint>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.Employee)
                      .WithMany() // الموظف ممكن يرسل أكثر من شكوى
                      .HasForeignKey(c => c.EmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Department)
                      .WithMany()
                      .HasForeignKey(c => c.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(c => c.Status)
                      .HasDefaultValue(ComplaintStatus.تحت_المراجعة); // القيمة الافتراضية عند إنشاء شكوى
            });

            // ====================== Seeding ======================

            // MaritalStatus
            modelBuilder.Entity<MaritalStatus>().HasData(
                new MaritalStatus { Id = 1, Name = "أعزب" },
                new MaritalStatus { Id = 2, Name = "متزوج" },
                new MaritalStatus { Id = 3, Name = "مطلق" },
                new MaritalStatus { Id = 4, Name = "أرمل" }
            );

            // JobTitle
            modelBuilder.Entity<JobTitle>().HasData(
                new JobTitle { Id = 1, Name = "موظف" },
                new JobTitle { Id = 2, Name = "فني" },
                new JobTitle { Id = 3, Name = "مدير" },
                new JobTitle { Id = 4, Name = "مأمور ضبظ" },
                new JobTitle { Id = 5, Name = "مأمور ضبظ قضائي" },
                new JobTitle { Id = 6, Name = "عضو تحقيق" }
            );

            // Roles
            modelBuilder.Entity<Role>().HasData(
     new Role { Id = 1, RoleName = "SuperAdmin" },
     new Role { Id = 2, RoleName = "HR" },
     new Role { Id = 3, RoleName = "DepartmentManager" },
     new Role { Id = 4, RoleName = "SubDepartmentManager" },
     new Role { Id = 5, RoleName = "SectionManager" },
     new Role { Id = 6, RoleName = "Employee" }
 );

            // ================= Permissions =================
            modelBuilder.Entity<Permission>().HasData(
                new Permission { Id = 1, PermissionName = "AddEmployee" },
                new Permission { Id = 2, PermissionName = "EditEmployee" },
                new Permission { Id = 3, PermissionName = "DeleteEmployee" },
                new Permission { Id = 4, PermissionName = "ViewEmployee" },
                new Permission { Id = 5, PermissionName = "ApproveLeave" },
                new Permission { Id = 6, PermissionName = "SubmitLeave" },
                new Permission { Id = 7, PermissionName = "SubmitComplaint" },
                new Permission { Id = 8, PermissionName = "ViewComplaints" },
                new Permission { Id = 9, PermissionName = "AssignTask" },
                new Permission { Id = 10, PermissionName = "ViewDepartmentEmployees" },
                new Permission { Id = 11, PermissionName = "AddOwnEducation" },
                new Permission { Id = 12, PermissionName = "EditOwnEducation" },
                new Permission { Id = 13, PermissionName = "ManageEmployeeEducation" },
                new Permission { Id = 14, PermissionName = "AssignRole" }

            );


            // Permissions
            modelBuilder.Entity<RolePermission>().HasData(

     // ================= SuperAdmin (كل شيء) =================
     new RolePermission { RoleId = 1, PermissionId = 1 },
     new RolePermission { RoleId = 1, PermissionId = 2 },
     new RolePermission { RoleId = 1, PermissionId = 3 },
     new RolePermission { RoleId = 1, PermissionId = 4 },
     new RolePermission { RoleId = 1, PermissionId = 5 },
     new RolePermission { RoleId = 1, PermissionId = 6 },
     new RolePermission { RoleId = 1, PermissionId = 7 },
     new RolePermission { RoleId = 1, PermissionId = 8 },
     new RolePermission { RoleId = 1, PermissionId = 9 },
     new RolePermission { RoleId = 1, PermissionId = 10 },
     new RolePermission { RoleId = 1, PermissionId = 11 },
     new RolePermission { RoleId = 1, PermissionId = 12 },
     new RolePermission { RoleId = 1, PermissionId = 13 },
new RolePermission { RoleId = 1, PermissionId = 14 },
// ================= SubDepartmentManager =================
new RolePermission { RoleId = 4, PermissionId = 5 },  // ApproveLeave
new RolePermission { RoleId = 4, PermissionId = 10 }, // ViewDepartmentEmployees

     // ================= Employee (افتراضي) =================
     new RolePermission { RoleId = 6, PermissionId = 4 },  // ViewEmployee
     new RolePermission { RoleId = 6, PermissionId = 6 },  // SubmitLeave
     new RolePermission { RoleId = 6, PermissionId = 11 }, // AddOwnEducation
     new RolePermission { RoleId = 6, PermissionId = 12 }, // EditOwnEducation

     // ================= SectionManager =================
     new RolePermission { RoleId = 5, PermissionId = 5 },  // ApproveLeave
     new RolePermission { RoleId = 5, PermissionId = 10 }, // ViewDepartmentEmployees

     // ================= DepartmentManager =================
     new RolePermission { RoleId = 3, PermissionId = 5 },
     new RolePermission { RoleId = 3, PermissionId = 10 },

     // ================= HR =================
     new RolePermission { RoleId = 2, PermissionId = 1 }, // AddEmployee
     new RolePermission { RoleId = 2, PermissionId = 2 }, // EditEmployee
     new RolePermission { RoleId = 2, PermissionId = 4 }  // ViewEmployee
 );


            // LeaveTypes
            modelBuilder.Entity<LeaveTypes>().HasData(
     new LeaveTypes
     {
         Id = 1,
         اسم_الاجازة = "إجازة سنوية",
         مخصومة_من_الرصيد = true,
         تحتاج_نموذج = false,
         IsAffectedByHolidays = true, // لا تحسب الجمعة والسبت
         مفعلة = true
     },
     new LeaveTypes
     {
         Id = 2,
         اسم_الاجازة = "إجازة مرضية",
         مخصومة_من_الرصيد = true,
         تحتاج_نموذج = true,
         IsAffectedByHolidays = true, // المريض لا تضيع عليه عطلته الأسبوعية
         مفعلة = true
     },
     new LeaveTypes
     {
         Id = 3,
         اسم_الاجازة = "إجازة حج",
         مخصومة_من_الرصيد = false,
         تحتاج_نموذج = true,
         IsAffectedByHolidays = false, // تحسب أيام متصلة (مدة محددة قانوناً)
         مفعلة = true
     },
     new LeaveTypes
     {
         Id = 4,
         اسم_الاجازة = "إجازة عمرة",
         مخصومة_من_الرصيد = false,
         تحتاج_نموذج = true,
         IsAffectedByHolidays = false, // أيام متصلة
         مفعلة = true
     },
     new LeaveTypes
     {
         Id = 5,
         اسم_الاجازة = "إجازة وضع",
         مخصومة_من_الرصيد = false,
         تحتاج_نموذج = true,
         IsAffectedByHolidays = false, // تحسب 3 أشهر أو 90 يوماً متصلة
         مفعلة = true
     },
     new LeaveTypes
     {
         Id = 6,
         اسم_الاجازة = "إجازة طارئه",
         مخصومة_من_الرصيد = true, // عادة في ليبيا تخصم من الرصيد السنوي
         تحتاج_نموذج = false,
         IsAffectedByHolidays = true, // تحسب أيام متصلة، لكن لا تحسب الجمعة والسبت
         مفعلة = true
     }
 );
        }
    }
}
