using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.QuizBank;

public class RatingRequest
{
    [Required]
    [Range(0, 5)]
    public int Star { get; set;}
}