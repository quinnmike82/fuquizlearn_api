namespace fuquizlearn_api.Entities
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public Classroom? Classroom { get; set; }
        public Account? Author { get; set; }
        public List<Comment>? Comments { get; set; }
        public string? GameLink { get; set; }
        public string? BankLink { get; set; }
        public int[]? ViewIds { get; set; } = new int[] {};
        public QuizBank? QuizBank { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Updated { get; set; }
    }
}
