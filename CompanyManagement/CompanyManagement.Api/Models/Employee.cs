using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompanyManagement.Api.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeNo { get; set; }
        
        [Required]
        [StringLength(50)]
        public string EmployeeName { get; set; } = string.Empty;
        
        [Required]
        public decimal Salary { get; set; }
        
        [Required]
        public int DepartmentNo { get; set; }
        
        public DateTime? LastModifyDate { get; set; }
        
        // Navigation property - nullable to avoid requiring it during creation
        [ForeignKey("DepartmentNo")]
        public virtual Department? Department { get; set; }
    }
}