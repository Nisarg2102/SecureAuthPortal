using System.ComponentModel.DataAnnotations;

namespace SecureAuthPortal.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string EmailId { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter valid 10-digit mobile number")]
        public string MobileNo { get; set; }
    }
}
