using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Models.QuizBank
{
    public class QuizBankResponse
    {
        public int Id { get; set; }
        public string BankName { get; set; }
        public string AuthorName { get; set; }
        public List<Quiz> Quizes { get; set; }
        public string? descrition { get; set; }
        public Visibility Visibility { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
