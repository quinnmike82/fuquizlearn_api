using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Quiz;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Classroom
{
    public class AnswerHistoryResponse
    {
        public GameQuizResponse Quiz { get; set; }
        public string[] UserAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }
}
