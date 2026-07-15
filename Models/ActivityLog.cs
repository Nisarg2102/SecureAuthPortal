using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureAuthPortal.Models
{
    [Table("ActivityLog")]
    public class ActivityLog
    {
        [Key]
        public long Id { get; set; }
        
        public string Username { get; set; }
        public string Role { get; set; }
        public string Activity { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string Status { get; set; } // Success / Failed
    }
}
