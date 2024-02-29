namespace fuquizlearn_api.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public Account? Author { get; set; }
        public int PostId { get; set; }
        public Post Post { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Deleted { get; set; }
    }
}
