using fuquizlearn_api.Entities;

namespace fuquizlearn_api.Models.Posts
{
    public class PostResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public Account? Author { get; set; }
        public List<Comment>? Comments { get; set; }
        public DateTime Created { get; set; } 
        public DateTime? Updated { get; set; }
    }
}
