namespace fuquizlearn_api.Entities
{
    public class PlanAccount
    {
        public int Id { get; set; }
        public Plan Plan { get; set; }
        public Account Account { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Cancelled { get; set; }
        public string TransactionId { get; set; }
        public int Duration { get; set; }
        public int Amount { get; set; }
    }
}
