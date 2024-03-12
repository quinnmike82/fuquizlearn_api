namespace fuquizlearn_api.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public Account? Account { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Read { get; set; }
        public DateTime? Deleted  { get; set; }
    }
}
