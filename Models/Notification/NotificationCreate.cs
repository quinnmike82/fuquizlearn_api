namespace fuquizlearn_api.Models.Notification
{
    public class NotificationCreate
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public int AccountId { get; set; }
    }
}
