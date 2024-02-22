using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Accounts;

public class RegisterRequest
{
    [Required] public string Username { get; set; }

    [Required] public string FullName { get; set; }

    [Required] [DataType(DataType.Date)] public DateTime Dob { get; set; }

    [Required] [EmailAddress] public string Email { get; set; }

    [Required] [MinLength(6)] public string Password { get; set; }
}