using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Models.Classroom
{
    public class GameQuizWithAnswerResponse
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public GameResponse Game { get; set; }
        public List<string> Questions { get; set; }
        public List<string> Answers { get; set; }
        public List<string> CorrectAnswers { get; set; }
        public GameQuizType Type { get; set; }
    }   
}
