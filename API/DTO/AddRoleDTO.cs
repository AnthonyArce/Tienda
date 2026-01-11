using System.ComponentModel.DataAnnotations;

namespace API.DTO
{
    public class AddRoleDTO
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; }
    }
}
