using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Quiz
{
    public class QuizUpdate
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
        public string? Explaination { get; set; }
    }
}
