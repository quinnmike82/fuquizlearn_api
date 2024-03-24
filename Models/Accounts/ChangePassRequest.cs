using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Accounts
{
    public class ChangePassRequest
    {
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
        [Required]
        [MinLength(6)]
        public string OldPassword { get; set; }
    }
}
