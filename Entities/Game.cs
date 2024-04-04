using fuquizlearn_api.Enum;
using fuquizlearn_api.Models.Quiz;

namespace fuquizlearn_api.Entities
{
    public class Game
    {
        public int Id { get; set; }
        public string GameName { get; set; }
        public bool IsTest { get; set; } = false;
        public int? ClassroomId { get; set; }
        public Classroom? Classroom { get; set; }
        public int QuizBankId { get; set; }
        public QuizBank QuizBank { get; set; }
        public List<GameQuiz>? GameQuizs { get; set; }
        public int NumberOfQuizzes { get; set; }
        public GameStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? Duration { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }
    }

    public class GameQuiz
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public Game Game { get; set; }
        public List<string> Questions { get; set; }
        public List<string> Answers { get; set; }
        public List<string> CorrectAnswers { get; set; }
        public GameQuizType Type { get; set; }
    }
}
