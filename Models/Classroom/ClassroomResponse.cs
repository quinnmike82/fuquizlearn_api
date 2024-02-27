using fuquizlearn_api.Entities;

namespace fuquizlearn_api.Models.Classroom
{
    public class ClassroomResponse
    {
        public int Id { get; set; }
        public string Classname { get; set; }
        public string Description { get; set; }
        public Account? Account { get; set; }
        public int[] BankIds { get; set; }
        public List<ClassroomCode> ClassroomCodes { get; set; }
    }
}
