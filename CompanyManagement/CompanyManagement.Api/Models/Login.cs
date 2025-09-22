using System.ComponentModel.DataAnnotations;

namespace CompanyManagement.Api.Models
{
    public class Login
    {
        [Key]
        public int LoginNo { get; set; }
        
        [Required]
        [StringLength(20)]
        public string LoginUserName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string LoginPassword { get; set; } = string.Empty;
    }
}