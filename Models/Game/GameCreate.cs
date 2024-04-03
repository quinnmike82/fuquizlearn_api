using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Classroom
{
    public class GameCreate
    {
        [Required]
        public string GameName { get; set; }
        public bool? IsTest { get; set; }
        public int? ClassroomId { get; set; }
        [Required]
        public int QuizBankId { get; set; }
        [Required]
        public int Amount { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }
        public int? Duration { get; set; }
        [Required]
        [MinLength(1)]
        public GameQuizType[] QuizTypes{ get; set; }
    }
}
