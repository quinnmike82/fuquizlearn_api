namespace fuquizlearn_api.Models.Notification
{
    public class NotificationUpdate
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string ObjectName { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Read { get; set; }
        public DateTime? Deleted { get; set; }
    }
}
