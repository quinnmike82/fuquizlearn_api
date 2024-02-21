using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Quiz
{
    public class QuizCreate
    {
        [Required]
        public string Question { get; set; }
        [Required]
        public string Answer{ get; set; }
        public string? Explaination { get; set; }
    }
}
