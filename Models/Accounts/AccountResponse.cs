namespace fuquizlearn_api.Models.Accounts
{
    public class AccountResponse
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public DateTime Dob { get; set; }
        public int useAICount { get; set; }
        public List<int> FavoriteBankIds { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public bool IsVerified { get; set; }
    }
}
