using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Models.Classroom
{
    public class GameResponse
    {
        public int Id { get; set; }
        public string GameName { get; set; }
        public bool IsTest { get; set; }
        public int ClassroomId { get; set; }
        public int QuizBankId { get; set; }
        public int NumberOfQuizzes { get; set; }
        public GameStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? Duration { get; set; }
        public DateTime Created { get; set; } 
        public DateTime? Updated { get; set; }
    }

    public class GameQuizResponse
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public GameResponse Game { get; set; }
        public List<string> Questions { get; set; }
        public List<string> Answers { get; set; }
        public GameQuizType Type { get; set; }
    }   
}
