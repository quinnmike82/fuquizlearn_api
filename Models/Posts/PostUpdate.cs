namespace fuquizlearn_api.Models.Posts
{
    public class PostUpdate
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string? GameLink { get; set; }
        public string? BankLink { get; set; }
    }
}
