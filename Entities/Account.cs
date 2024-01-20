using System.Data;

namespace fuquizlearn_api.Entities
{
    public class Account
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Avatar { get; set; }
        public DateTime Dob { get; set; }
        public int useAICount { get; set; }
        public string PasswordHash { get; set; }
        public Role Role { get; set; }
        public string? VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }
        public List<int>? FavoriteBankIds { get; set; }
        public DateTime? isBan { get; set; } = null;
        public DateTime? isWarning { get; set; } = null;

        public bool OwnsToken(string token)
        {
            return RefreshTokens?.Find(x => x.Token == token) != null;
        }
    }
}
