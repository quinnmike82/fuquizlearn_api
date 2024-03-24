using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;

namespace fuquizlearn_api.Models.Classroom
{
    public class ClassroomResponse
    {
        public int Id { get; set; }
        public string Classname { get; set; }
        public string Description { get; set; }
        public AccountResponse Author { get; set; }
        public int[] BankIds { get; set; }
        public int[]? AccountIds { get; set; }
        public int[]? BanMembers { get; set; }
        public DateTime Created { get; set; }
        public int StudentNumber { get; set; }
        public bool isStudentAllowInvite { get; set; }
    }
}
