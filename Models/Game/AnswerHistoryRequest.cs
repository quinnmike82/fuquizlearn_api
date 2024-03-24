using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Classroom
{
    public class AnswerHistoryRequest
    {
        [Required]
        public int QuizId { get; set; }
        [Required]
        public string UserAnswer { get; set; }
    }
}
