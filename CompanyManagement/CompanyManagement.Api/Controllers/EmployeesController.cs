using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompanyManagement.Api.Data;
using CompanyManagement.Api.Models;
using System.Text;

namespace CompanyManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly CompanyDbContext _context;

        public EmployeesController(CompanyDbContext context)
        {
            _context = context;
        }

        // GET: api/employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            return await _context.Employees.Include(e => e.Department).ToListAsync();
        }

        // GET: api/employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeNo == id);

            if (employee == null)
            {
                return NotFound();
            }

            return employee;
        }

        // POST: api/employees
        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify department exists
            var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentNo == employee.DepartmentNo);
            if (!departmentExists)
            {
                return BadRequest("Invalid department.");
            }

            employee.LastModifyDate = DateTime.Now;
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeNo }, employee);
        }

        // PUT: api/employees/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            if (id != employee.EmployeeNo)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify department exists
            var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentNo == employee.DepartmentNo);
            if (!departmentExists)
            {
                return BadRequest("Invalid department.");
            }

            employee.LastModifyDate = DateTime.Now;
            _context.Entry(employee).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/employees/export
        [HttpGet("export")]
        public async Task<IActionResult> ExportEmployees()
        {
            var employees = await _context.Employees.ToListAsync();
            
            var result = ExportEmployeesToTextFormat(employees);
            
            return File(Encoding.UTF8.GetBytes(result), "text/plain", "employees.txt");
        }

        // POST: api/employees/5/update-salary
        [HttpPost("{id}/update-salary")]
        public async Task<IActionResult> UpdateSalary(int id, [FromBody] SalaryUpdateRequest request)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var newSalary = employee.Salary * (1 + request.PercentageChange / 100m);
            employee.Salary = newSalary;
            employee.LastModifyDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { EmployeeNo = employee.EmployeeNo, OldSalary = employee.Salary / (1 + request.PercentageChange / 100m), NewSalary = newSalary });
        }

        private string ExportEmployeesToTextFormat(List<Employee> employees)
        {
            var sb = new StringBuilder();
            
            // Header
            sb.AppendLine("+----------+------------------+------------+------------+");
            sb.AppendLine("|employeeNo|employeeName      |Salary      |departmentNo|");
            sb.AppendLine("+----------+------------------+------------+------------+");
            
            // Data rows
            foreach (var employee in employees)
            {
                sb.AppendLine($"|{employee.EmployeeNo,-10}|{employee.EmployeeName,-18}|{employee.Salary,-12:N0}|{employee.DepartmentNo,-12}|");
                sb.AppendLine("+----------+------------------+------------+------------+");
            }
            
            return sb.ToString();
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeNo == id);
        }
    }

    public class SalaryUpdateRequest
    {
        public decimal PercentageChange { get; set; }
    }
}