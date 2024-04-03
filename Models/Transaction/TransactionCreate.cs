namespace fuquizlearn_api.Models.Transaction
{
    public class TransactionCreate
    {
        public string TransactionId { get; set; }
        public string TransactionType { get; set; }
        public string Email { get; set; }
        public int Amount { get; set; }
    }
}
