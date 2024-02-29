using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;

namespace fuquizlearn_api.Models.Posts
{
    public class PostResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public AccountResponse? Author { get; set; }
        public List<CommentResponse>? Comments { get; set; }
        public DateTime Created { get; set; } 
        public DateTime? Updated { get; set; }
    }
}
