using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.QuizBank;

public class SaveProgressRequest
{
    [Required]
    public List<int> LearnedQuizIds { get; set; }
    [Required]
    public int CurrentQuizId { get; set; }
    [Required]
    public LearnMode LearnMode { get; set; }
}