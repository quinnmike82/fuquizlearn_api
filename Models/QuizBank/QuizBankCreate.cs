﻿using fuquizlearn_api.Enum;
using fuquizlearn_api.Models.Quiz;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.QuizBank
{
    public class QuizBankCreate
    {
        [Required]
        public string BankName { get; set; }
        [Required]
        public QuizCreate[] Quizes { get; set; }
        public string? descrition { get; set; }
        public Visibility? Visibility { get; set; }
    }
}
