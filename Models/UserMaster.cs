using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureAuthPortal.Models
{
    [Table("UserMaster")]
    public class UserMaster
    {
        [Key]
        public long UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmailId { get; set; }
        public string MobileNo { get; set; }
        public DateTime DOB { get; set; }
        public string Gender { get; set; }
        public DateTime CreatedDate { get; set; }

        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;

        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }

        [ForeignKey("Role")]
        public long RoleId { get; set; }
        public virtual RoleMaster Role { get; set; }

        public virtual ICollection<DocumentMaster> UploadedDocuments { get; set; }

        public virtual ICollection<DocumentMaster> ApprovedDocuments { get; set; }
    }
}