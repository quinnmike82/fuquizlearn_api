using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Quiz;

namespace fuquizlearn_api.Models.QuizBank;

public class QuizBankResponse
{
    public int Id { get; set; }
    public string BankName { get; set; }
    public AccountResponse Author { get; set; }
    public List<QuizResponse> Quizes { get; set; }
    public string? Description { get; set; }
    public Visibility Visibility { get; set; }
    public List<Rating> Rating { get; set; }
    public double AverageRating {  get; set; }
    public List<string> Tags { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
}