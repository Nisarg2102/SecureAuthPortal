using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureAuthPortal.Models
{
    [Table("ErrorLog")]
    public class ErrorLog
    {
        [Key]
        public long Id { get; set; }
        
        public string Username { get; set; }
        public string ErrorMessage { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string StackTrace { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
    }
}
