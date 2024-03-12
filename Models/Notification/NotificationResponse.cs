using fuquizlearn_api.Models.Accounts;

namespace fuquizlearn_api.Models.Notification
{
    public class NotificationResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Read { get; set; }
    }
}
