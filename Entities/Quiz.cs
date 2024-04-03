using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;
using Pgvector;

namespace fuquizlearn_api.Entities
{
    public class Quiz
    {
        public int Id { get; set; }
        public int QuizBankId { get; set; }
        public QuizBank QuizBank { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string? Explaination { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }

        [Column(TypeName = "vector(768)")] public Vector? Embedding { get; set; }
    }
}