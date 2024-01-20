using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Accounts
{
    public class AuthenticateRequest
    {
        [Required]
        public string EmailOrUsername { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
