using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Entities
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public List<Choice> Choices { get; set; }
        public string? Explaination { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
    public class Choice
    {
        public string ChoiceContent { get; set; }
        public bool IsAnswer { get; set;}
    }
}
