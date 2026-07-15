using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureAuthPortal.Models
{
    /// <summary>
    /// Document Master Model
    /// 
    /// Stores information about user documents:
    /// - Aadhar Card
    /// - PAN Card
    /// - Other Documents
    /// 
    /// Status flow: Pending -> Approved/Rejected
    /// </summary>
    [Table("DocumentMaster")]
    public class DocumentMaster
    {
        [Key]
        public long DocumentId { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required(ErrorMessage = "Document Type is required")]
        [StringLength(50)]
        public string DocumentType { get; set; } // Aadhar, PAN, Other

        [StringLength(500)]
        public string? DocumentPath { get; set; }

        [Required]
        [StringLength(100)]
        public string FileName { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        public byte[]? FileData { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [StringLength(500)]
        public string? VerificationNotes { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public long? ApprovedBy { get; set; } // Admin who approved

        public DateTime? ApprovedDate { get; set; }

        // Navigation property
        public virtual UserMaster User { get; set; }

        public virtual UserMaster? ApprovedByUser { get; set; }
    }
}
