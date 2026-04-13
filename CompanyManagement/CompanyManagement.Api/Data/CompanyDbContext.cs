using Microsoft.EntityFrameworkCore;
using CompanyManagement.Api.Models;

namespace CompanyManagement.Api.Data
{
    public class CompanyDbContext : DbContext
    {
        public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
        {
        }

        public DbSet<Login> Logins { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Employee-Department relationship
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentNo)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for Salary
            modelBuilder.Entity<Employee>()
                .Property(e => e.Salary)
                .HasColumnType("decimal(18,2)");

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Login data
            modelBuilder.Entity<Login>().HasData(
                new Login { LoginNo = 1, LoginUserName = "Bill", LoginPassword = "ItsNotSoft" },
                new Login { LoginNo = 2, LoginUserName = "Jean", LoginPassword = "trollsRule" }
            );

            // Seed Department data
            modelBuilder.Entity<Department>().HasData(
                new Department { DepartmentNo = 1, DepartmentName = "Development", DepartmentLocation = "London" },
                new Department { DepartmentNo = 2, DepartmentName = "Development", DepartmentLocation = "Zurich" },
                new Department { DepartmentNo = 3, DepartmentName = "Development", DepartmentLocation = "Osijek" },
                new Department { DepartmentNo = 4, DepartmentName = "Sales", DepartmentLocation = "London" },
                new Department { DepartmentNo = 5, DepartmentName = "Sales", DepartmentLocation = "Zurich" },
                new Department { DepartmentNo = 6, DepartmentName = "Sales", DepartmentLocation = "Osijek" },
                new Department { DepartmentNo = 7, DepartmentName = "Sales", DepartmentLocation = "Basel" },
                new Department { DepartmentNo = 8, DepartmentName = "Sales", DepartmentLocation = "Lugano" }
            );

            // Seed Employee data
            modelBuilder.Entity<Employee>().HasData(
                new Employee { EmployeeNo = 1, EmployeeName = "Fred Davies", Salary = 50000, DepartmentNo = 4 },
                new Employee { EmployeeNo = 2, EmployeeName = "Bernard Katic", Salary = 50000, DepartmentNo = 3 },
                new Employee { EmployeeNo = 3, EmployeeName = "Rich Davies", Salary = 30000, DepartmentNo = 5 },
                new Employee { EmployeeNo = 4, EmployeeName = "Eva Dobos", Salary = 30000, DepartmentNo = 6 },
                new Employee { EmployeeNo = 5, EmployeeName = "Mario Hunjadi", Salary = 25000, DepartmentNo = 8 },
                new Employee { EmployeeNo = 6, EmployeeName = "Jean Michele", Salary = 25000, DepartmentNo = 7 },
                new Employee { EmployeeNo = 7, EmployeeName = "Bill Gates", Salary = 25000, DepartmentNo = 1 },
                new Employee { EmployeeNo = 8, EmployeeName = "Maja Janic", Salary = 30000, DepartmentNo = 3 },
                new Employee { EmployeeNo = 9, EmployeeName = "Igor Horvat", Salary = 35000, DepartmentNo = 3 }
            );
        }
    }
}