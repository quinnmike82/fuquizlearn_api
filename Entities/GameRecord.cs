namespace fuquizlearn_api.Entities
{
    public class GameRecord
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game Game { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }
        public List<AnswerHistory> AnswerHistories { get; set; }
        public int TotalMark => AnswerHistories.Count(a => a.IsCorrect);
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }
    }

    public class AnswerHistory
    {
        public int Id { get; set; }
        public int GameRecordId { get; set; }
        public GameRecord GameRecord { get; set; }
        public int GameQuizId { get; set; }
        public GameQuiz GameQuiz { get; set; }
        public string[] UserAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }
}
