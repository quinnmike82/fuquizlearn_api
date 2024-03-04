using fuquizlearn_api.Entities;

namespace fuquizlearn_api.Models.Posts
{
    public class PostCreate
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public int ClassroomId { get; set; }
        public string? GameLink { get; set; }
        public string? BankLink { get; set; }
    }
}
