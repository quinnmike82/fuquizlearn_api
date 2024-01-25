using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Models.QuizBank
{
    public class QuizBankUpdate
    {
        public string? BankName { get; set; }
        public QuizCreate[]? Quizes { get; set; }
        public string? descrition { get; set; }
        public Visibility? Visibility { get; set; }
    }
}
