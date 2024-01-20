using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Accounts
{
    public class UpdateRequest
    {
        public string? Username { get; set; }
        public string? FullName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Dob { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? Avatar { get; set; }

        [EnumDataType(typeof(Role))]
        public string? Role { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }

        [Compare("Password")]
        public string? ConfirmPassword { get; set; }
    }
}
