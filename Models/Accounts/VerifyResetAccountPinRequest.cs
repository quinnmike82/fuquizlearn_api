using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Accounts;

public class VerifyResetAccountPinRequest
{
    [Required] [StringLength(6)] public string Pin { get; set; }

    [Required] [EmailAddress] public string Email { get; set; }
}