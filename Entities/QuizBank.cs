using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Entities;

public class QuizBank
{
    public int Id { get; set; }
    public string BankName { get; set; }
    public Account Author { get; set; }
    public List<Quiz> Quizes { get; set; }
    public string? Description { get; set; }
    public Visibility Visibility { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime? Updated { get; set; }
}