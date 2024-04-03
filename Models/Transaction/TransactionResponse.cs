using fuquizlearn_api.Models.Accounts;

namespace fuquizlearn_api.Models.Transaction
{
    public class TransactionResponse
    {
        public int Id { get; set; }
        public string TransactionId { get; set; }
        public string TransactionType { get; set; }
        public string Email { get; set; }
        public AccountResponse Account { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public int Amount { get; set; }
    }
}
