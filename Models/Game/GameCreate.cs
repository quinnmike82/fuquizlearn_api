using fuquizlearn_api.Entities;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Classroom
{
    public class GameCreate
    {
        [Required]
        public string GameName { get; set; }
        [Required]
        public int ClassroomId { get; set; }
        [Required]
        [MinLength(1)]
        public List<int> QuizIds { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }
    }
}
