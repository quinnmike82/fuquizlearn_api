namespace fuquizlearn_api.Entities
{
    public class ClassroomMember
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public Account? Account { get; set; }
        public int ClassroomId { get; set; }
        public Classroom? Classroom { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
    }
}
