﻿using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Accounts
{
    public class CreateRequest
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime Dob { get; set; }
        [Required]
        [EnumDataType(typeof(Role))]
        public string Role { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}
