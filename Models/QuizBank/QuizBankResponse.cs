using fuquizlearn_api.Enum;
using fuquizlearn_api.Models.Quiz;

namespace fuquizlearn_api.Models.QuizBank
{
    public class QuizBankResponse
    {
        public int Id { get; set; }
        public string BankName { get; set; }
        public string AuthorName { get; set; }
        public List<QuizResponse> Quizes { get; set; }
        public string? descrition { get; set; }
        public Visibility Visibility { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
