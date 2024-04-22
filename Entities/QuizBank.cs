using fuquizlearn_api.Enum;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;
using Pgvector;

namespace fuquizlearn_api.Entities;

public class QuizBank
{
    public int Id { get; set; }
    public string BankName { get; set; }
    public Account Author { get; set; }
    public List<Quiz> Quizes { get; set; }
    public string? Description { get; set; }
    public Visibility Visibility { get; set; }
    public List<Rating>? Rating{ get; set; }
    public double AverageRating => (Rating != null && Rating.Count > 0) ? Rating.Average(r => r.Star): 0 ;
    public List<string>? Tags { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
    public DateTime? DeletedAt { get; set; }
   
    [Column(TypeName = "vector(768)")]   
    public Vector? Embedding { get; set; }
}

public class Rating
{
    public int AccountId { get; set; }
    public int Star { get; set; }

    public Rating(int accountId, int star)
    {
        AccountId = accountId;
        Star = star;
    }
}