using fuquizlearn_api.Entities;

namespace fuquizlearn_api.Models.Classroom
{
    public class ClassroomUpdate
    {
        public int Id { get; set; }
        public string Classname { get; set; }
        public string Description { get; set; }
        public bool isStudentAllowInvite { get; set; } = false;
    }
}
