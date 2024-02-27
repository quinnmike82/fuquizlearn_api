using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Entities
{
    public class Quiz
    {
        public int Id { get; set; }
        public int QuizBankId { get; set; }
        public QuizBank QuizBank { get; set; }
        public string Question { get; set; }
        public string Answer{ get; set; }
        public string? Explaination { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? Updated { get; set; }
    }
}
