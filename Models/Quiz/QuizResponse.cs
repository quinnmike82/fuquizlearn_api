using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Quiz
{
    public class QuizResponse
    {
        public int Id { get; set; }
        public int QuizBankId { get; set; }
        public string Question { get; set; }
        public List<Choice> Choices { get; set; }
        public string? Explaination { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
