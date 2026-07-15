using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureAuthPortal.Models
{
    [Table("RoleMaster")]
    public class RoleMaster
    {
        [Key]
        public long RoleId { get; set; }

        [Required(ErrorMessage = "Role Name is required")]
        [StringLength(50)]
        public string RoleName { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation property
        public ICollection<UserMaster> Users { get; set; } = new List<UserMaster>();
    }
}