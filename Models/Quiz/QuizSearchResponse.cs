using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.QuizBank;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Quiz
{
    public class QuizSearchResponse
    {
        public int Id { get; set; }
        public QuizBankResponse QuizBank { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string? Explaination { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
