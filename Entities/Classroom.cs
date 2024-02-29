using System.ComponentModel.DataAnnotations.Schema;

namespace fuquizlearn_api.Entities
{
    public class Classroom
    {
        public int Id { get; set; }
        public string Classname { get; set; }
        public string Description { get; set; }
        public Account Account { get; set; }
        public int[]? BankIds { get; set; } = new int[0];
        public List<ClassroomCode> ClassroomCodes { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public int[]? AccountIds { get; set; } = new int[0];
        public DateTime? DeletedAt { get; set; }
        public bool OwnsToken(string code)
        {
            return ClassroomCodes?.Find(x => x.Code == code) != null;
        }
    }
}
