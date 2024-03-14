namespace fuquizlearn_api.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionId { get; set; }
        public string TransactionType { get; set; }
        public Account Account { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public int Amount { get; set; }
    }
}
