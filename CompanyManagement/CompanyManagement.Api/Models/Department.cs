using System.ComponentModel.DataAnnotations;

namespace CompanyManagement.Api.Models
{
    public class Department
    {
        [Key]
        public int DepartmentNo { get; set; }
        
        [Required]
        [StringLength(20)]
        public string DepartmentName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string DepartmentLocation { get; set; } = string.Empty;
        
        // Navigation property
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}