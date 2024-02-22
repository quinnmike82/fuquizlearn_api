using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Accounts;

public class VerifyEmailRequest
{
    [Required] public string Token { get; set; }

    [Required] public string Email { get; set; }
}