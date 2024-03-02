using fuquizlearn_api.Enum;
using fuquizlearn_api.Models.Quiz;

namespace fuquizlearn_api.Models.QuizBank
{
    public class QuizBankUpdate
    {
        public string? BankName { get; set; }
        public QuizCreate[]? Quizes { get; set; }
        public string? Description { get; set; }
        public List<string>? Tags { get; set; }
        public Visibility? Visibility { get; set; }
    }
}
