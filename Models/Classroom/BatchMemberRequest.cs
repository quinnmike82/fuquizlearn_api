using System.ComponentModel.DataAnnotations;

namespace fuquizlearn_api.Models.Classroom
{
    public class BatchMemberRequest
    {
        [Required]
        [MinLength(1)]
        public List<int> MemberIds { get; set; }
    }
}
