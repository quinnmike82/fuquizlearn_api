using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.QuizBank
{
    public class QuizUpdate
    {
        public string? Question { get; set; }
        public List<Choice>? Choices { get; set; }
        public string? Explaination { get; set; }
    }
}
