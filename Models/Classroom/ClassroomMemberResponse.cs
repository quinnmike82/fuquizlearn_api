using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;

namespace fuquizlearn_api.Models.Classroom
{
    public class ClassroomMemberResponse
    {
        public AccountResponse Account { get; set; }
        public ClassroomResponse Classroom { get; set; }
        public DateTime JoinDate { get; set; }
    }
}
