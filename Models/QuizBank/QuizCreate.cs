using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.QuizBank
{
    public class QuizCreate
    {
        [Required]
        public string Question { get; set; }
        [Required]
        public Choice[] Choices { get; set; }
        public string? Explaination { get; set; }
    }
}
