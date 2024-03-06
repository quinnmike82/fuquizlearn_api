using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Entities
{
    public class ProgressResponse
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int QuizBankId { get; set; }
        public List<int> LearnedQuizIds { get; set; }
        public int CurrentQuizId { get; set; }
        public LearnMode LearnMode { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
