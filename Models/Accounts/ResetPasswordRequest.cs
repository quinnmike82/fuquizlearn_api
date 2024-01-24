using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Accounts;

public class ResetPasswordRequest
{
    [Required] public string Token { get; set; }

    [Required] [MinLength(6)] public string Password { get; set; }
}