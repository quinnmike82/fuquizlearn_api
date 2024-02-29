using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;

namespace fuquizlearn_api.Models.Posts
{
    public class CommentResponse
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public AccountResponse Author { get; set; }
        public int PostId { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Deleted { get; set; }
    }
}
