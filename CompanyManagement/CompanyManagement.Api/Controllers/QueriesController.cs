using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompanyManagement.Api.Data;

namespace CompanyManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QueriesController : ControllerBase
    {
        private readonly CompanyDbContext _context;

        public QueriesController(CompanyDbContext context)
        {
            _context = context;
        }

        // GET: api/queries/average-development-salary-excluding-london
        [HttpGet("average-development-salary-excluding-london")]
        public async Task<ActionResult<object>> GetAverageDevelopmentSalaryExcludingLondon()
        {
            var averageSalary = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Department.DepartmentName == "Development" && e.Department.DepartmentLocation != "London")
                .AverageAsync(e => e.Salary);

            return Ok(new { AverageSalary = averageSalary, Query = "Average salary for Development employees excluding London" });
        }

        // GET: api/queries/locations-with-multiple-employees
        [HttpGet("locations-with-multiple-employees")]
        public async Task<ActionResult<object>> GetLocationsWithMultipleEmployees()
        {
            var locations = await _context.Employees
                .Include(e => e.Department)
                .GroupBy(e => e.Department.DepartmentLocation)
                .Where(g => g.Count() > 1)
                .Select(g => new { Location = g.Key, EmployeeCount = g.Count() })
                .ToListAsync();

            return Ok(new { Locations = locations, Query = "Locations with more than one employee" });
        }

        // GET: api/queries/development-employees-by-location
        [HttpGet("development-employees-by-location")]
        public async Task<ActionResult<object>> GetDevelopmentEmployeesByLocation()
        {
            // Get all locations
            var allLocations = await _context.Departments
                .Select(d => d.DepartmentLocation)
                .Distinct()
                .ToListAsync();

            // Get development employee counts by location
            var developmentCounts = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Department.DepartmentName == "Development")
                .GroupBy(e => e.Department.DepartmentLocation)
                .Select(g => new { Location = g.Key, EmployeeCount = g.Count() })
                .ToListAsync();

            // Combine to show all locations (including those with 0 development employees)
            var result = allLocations.Select(location => new
            {
                Location = location,
                DevelopmentEmployeeCount = developmentCounts.FirstOrDefault(dc => dc.Location == location)?.EmployeeCount ?? 0
            }).ToList();

            return Ok(new { LocationCounts = result, Query = "Development employees count by location (including locations with 0)" });
        }

        // GET: api/queries/second-highest-salary
        [HttpGet("second-highest-salary")]
        public async Task<ActionResult<object>> GetSecondHighestSalary()
        {
            var secondHighestSalary = await _context.Employees
                .OrderByDescending(e => e.Salary)
                .Skip(1)
                .Take(1)
                .Select(e => e.Salary)
                .FirstOrDefaultAsync();

            return Ok(new { SecondHighestSalary = secondHighestSalary, Query = "Second highest salary" });
        }

        // GET: api/queries/department-view
        [HttpGet("department-view")]
        public async Task<ActionResult<object>> GetDepartmentView()
        {
            // Simulating the vwDepartment view
            var departmentView = await _context.Departments
                .Select(d => new
                {
                    DepartmentNo = d.DepartmentNo,
                    DepartmentDescription = d.DepartmentName + " " + d.DepartmentLocation
                })
                .ToListAsync();

            return Ok(new { DepartmentView = departmentView, Query = "Department view with concatenated description" });
        }
    }
}