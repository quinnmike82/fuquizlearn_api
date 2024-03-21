using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Models.Classroom
{
    public class GameResponse
    {
        public int Id { get; set; }
        public string GameName { get; set; }
        public int ClassroomId { get; set; }
        public List<GameQuiz>? GameQuizs { get; set; }
        public GameStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime Created { get; set; } 
        public DateTime? Updated { get; set; }
    }
}
