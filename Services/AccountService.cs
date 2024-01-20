
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Accounts;
using AutoMapper;
using BC = BCrypt.Net.BCrypt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using fuquizlearn_api.Authorization;
using System.Reflection;

namespace fuquizlearn_api.Services
{
    public interface IAccountService
    {
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        AuthenticateResponse RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        void Register(RegisterRequest model, string origin);
        void VerifyEmail(string token);
        void ForgotPassword(ForgotPasswordRequest model, string origin);
        void ValidateResetToken(ValidateResetTokenRequest model);
        void ResetPassword(ResetPasswordRequest model);
        IEnumerable<AccountResponse> GetAll();
        AccountResponse GetById(int id);
        AccountResponse Create(CreateRequest model);
        AccountResponse Update(int id, UpdateRequest model);
        void Delete(int id);
        void BanAccount(int id, string origin);
        void WarningAccount(int id, string origin);
    }

    public class AccountService : IAccountService
    {
        private readonly DataContext _context;
        private readonly IJwtUtils _jwtUtils;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;
        private readonly IEmailService _emailService;

        public AccountService(
            DataContext context,
            IJwtUtils jwtUtils,
            IMapper mapper,
            IOptions<AppSettings> appSettings,
            IEmailService emailService)
        {
            _context = context;
            _jwtUtils = jwtUtils;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _emailService = emailService;
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var account = _context.Accounts.FirstOrDefault(x => x.Email == model.EmailOrUsername || x.Username == model.EmailOrUsername);

            // validate
            if (account == null || !account.IsVerified || !BC.Verify(model.Password, account.PasswordHash))
                throw new AppException("Email or password is incorrect");

            // authentication successful so generate jwt and refresh tokens
            var jwtToken = _jwtUtils.GenerateJwtToken(account);
            var refreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
            account.RefreshTokens.Add(refreshToken);

            // remove old refresh tokens from account
            removeOldRefreshTokens(account);

            // save changes to db
            _context.Update(account);
            _context.SaveChanges();

            var response = _mapper.Map<AuthenticateResponse>(account);
            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;
            return response;
        }

        public AuthenticateResponse RefreshToken(string token, string ipAddress)
        {
            var account = getAccountByRefreshToken(token);
            var refreshToken = account.RefreshTokens.Single(x => x.Token == token);

            if (refreshToken.IsRevoked)
            {
                // revoke all descendant tokens in case this token has been compromised
                revokeDescendantRefreshTokens(refreshToken, account, ipAddress, $"Attempted reuse of revoked ancestor token: {token}");
                _context.Update(account);
                _context.SaveChanges();
            }

            if (!refreshToken.IsActive)
                throw new AppException("Invalid token");

            // replace old refresh token with a new one (rotate token)
            var newRefreshToken = rotateRefreshToken(refreshToken, ipAddress);
            account.RefreshTokens.Add(newRefreshToken);


            // remove old refresh tokens from account
            removeOldRefreshTokens(account);

            // save changes to db
            _context.Update(account);
            _context.SaveChanges();

            // generate new jwt
            var jwtToken = _jwtUtils.GenerateJwtToken(account);

            // return data in authenticate response object
            var response = _mapper.Map<AuthenticateResponse>(account);
            response.JwtToken = jwtToken;
            response.RefreshToken = newRefreshToken.Token;
            return response;
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var account = getAccountByRefreshToken(token);
            var refreshToken = account.RefreshTokens.Single(x => x.Token == token);

            if (!refreshToken.IsActive)
                throw new AppException("Invalid token");

            // revoke token and save
            revokeRefreshToken(refreshToken, ipAddress, "Revoked without replacement");
            _context.Update(account);
            _context.SaveChanges();
        }

        public void Register(RegisterRequest model, string origin)
        {
            // validate
            if (_context.Accounts.Any(x => x.Email == model.Email))
            {
                // send already registered error in email to prevent account enumeration
                sendAlreadyRegisteredEmail(model.Email, origin);
                throw new AppException("Email is already existed");
            }
            if (_context.Accounts.Any(x => x.Username == model.Username))
            {
                // send already registered error in email to prevent account enumeration
                sendAlreadyRegisteredEmail(model.Email, origin);
                throw new AppException("Email is already existed");
            }
            if(model.Avatar != null)
            {
                // upload image and save image url
            }


            // map model to new account object
            var account = _mapper.Map<Account>(model);

            // first registered account is an admin
            var isFirstAccount = _context.Accounts.Count() == 0;
            account.Role = isFirstAccount ? Role.Admin : Role.User;
            account.Created = DateTime.UtcNow;
            account.VerificationToken = generateVerificationToken();

            // hash password
            account.PasswordHash = BC.HashPassword(model.Password);

            // save account
            _context.Accounts.Add(account);
            _context.SaveChanges();
            Console.WriteLine(account.VerificationToken);
            // send email
            sendVerificationEmail(account, origin);
        }

        public void VerifyEmail(string token)
        {
            var account = _context.Accounts.SingleOrDefault(x => x.VerificationToken == token);

            if (account == null)
                throw new AppException("Verification failed");

            account.Verified = DateTime.UtcNow;
            account.VerificationToken = null;

            _context.Accounts.Update(account);
            _context.SaveChanges();
        }

        public void ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            var account = _context.Accounts.SingleOrDefault(x => x.Email == model.Email);

            // always return ok response to prevent email enumeration
            if (account == null) return;

            // create reset token that expires after 1 day
            account.ResetToken = generateResetToken();
            account.ResetTokenExpires = DateTime.UtcNow.AddDays(1);

            _context.Accounts.Update(account);
            _context.SaveChanges();

            // send email
            sendPasswordResetEmail(account, origin);
        }

        public void ValidateResetToken(ValidateResetTokenRequest model)
        {
            getAccountByResetToken(model.Token);
        }

        public void ResetPassword(ResetPasswordRequest model)
        {
            var account = getAccountByResetToken(model.Token);

            // update password and remove reset token
            account.PasswordHash = BC.HashPassword(model.Password);
            account.PasswordReset = DateTime.UtcNow;
            account.ResetToken = null;
            account.ResetTokenExpires = null;

            _context.Accounts.Update(account);
            _context.SaveChanges();
        }

        public IEnumerable<AccountResponse> GetAll()
        {
            var accounts = _context.Accounts;
            return _mapper.Map<IList<AccountResponse>>(accounts);
        }

        public AccountResponse GetById(int id)
        {
            var account = getAccount(id);
            return _mapper.Map<AccountResponse>(account);
        }

        public AccountResponse Create(CreateRequest model)
        {
            // validate
            if (_context.Accounts.Any(x => x.Email == model.Email))
                throw new AppException($"Email '{model.Email}' is already registered");
            if (_context.Accounts.Any(x => x.Username == model.Username))
                throw new AppException($"Username '{model.Username}' is already registered");

            // map model to new account object
            var account = _mapper.Map<Account>(model);
            account.Created = DateTime.UtcNow;
            account.Verified = DateTime.UtcNow;

            // hash password
            account.PasswordHash = BC.HashPassword(model.Password);

            // save account
            _context.Accounts.Add(account);
            _context.SaveChanges();

            return _mapper.Map<AccountResponse>(account);
        }

        public AccountResponse Update(int id, UpdateRequest model)
        {
            var account = getAccount(id);

            // validate
            if (account.Email != model.Email && _context.Accounts.Any(x => x.Email == model.Email))
                throw new AppException($"Email '{model.Email}' is already registered");

            // hash password if it was entered
            if (!string.IsNullOrEmpty(model.Password))
                account.PasswordHash = BC.HashPassword(model.Password);

            // copy model to account and save
            _mapper.Map(model, account);
            account.Updated = DateTime.UtcNow;
            _context.Accounts.Update(account);
            _context.SaveChanges();

            return _mapper.Map<AccountResponse>(account);
        }

        public void Delete(int id)
        {
            var account = getAccount(id);
            _context.Accounts.Remove(account);
            _context.SaveChanges();
        }

        // helper methods

        private Account getAccount(int id)
        {
            var account = _context.Accounts.Find(id);
            if (account == null) throw new KeyNotFoundException("Account not found");
            return account;
        }

        private Account getAccountByRefreshToken(string token)
        {
            var account = _context.Accounts.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
            if (account == null) throw new AppException("Invalid token");
            return account;
        }

        private Account getAccountByResetToken(string token)
        {
            var account = _context.Accounts.SingleOrDefault(x =>
                x.ResetToken == token && x.ResetTokenExpires > DateTime.UtcNow);
            if (account == null) throw new AppException("Invalid token");
            return account;
        }

        private string generateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string generateResetToken()
        {
            // token is a cryptographically strong random sequence of values
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

            // ensure token is unique by checking against db
            var tokenIsUnique = !_context.Accounts.Any(x => x.ResetToken == token);
            if (!tokenIsUnique)
                return generateResetToken();

            return token;
        }

        private string generateVerificationToken()
        {
            // token is a cryptographically strong random sequence of values
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

            // ensure token is unique by checking against db
            var tokenIsUnique = !_context.Accounts.Any(x => x.VerificationToken == token);
            if (!tokenIsUnique)
                return generateVerificationToken();

            return token;
        }

        private RefreshToken rotateRefreshToken(RefreshToken refreshToken, string ipAddress)
        {
            var newRefreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
            revokeRefreshToken(refreshToken, ipAddress, "Replaced by new token", newRefreshToken.Token);
            return newRefreshToken;
        }

        private void removeOldRefreshTokens(Account account)
        {
            account.RefreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.Created.AddDays(_appSettings.RefreshTokenTTL) <= DateTime.UtcNow);
        }

        private void revokeDescendantRefreshTokens(RefreshToken refreshToken, Account account, string ipAddress, string reason)
        {
            // recursively traverse the refresh token chain and ensure all descendants are revoked
            if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
            {
                var childToken = account.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);
                if (childToken.IsActive)
                    revokeRefreshToken(childToken, ipAddress, reason);
                else
                    revokeDescendantRefreshTokens(childToken, account, ipAddress, reason);
            }
        }

        private void revokeRefreshToken(RefreshToken token, string ipAddress, string reason = null, string replacedByToken = null)
        {
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
            token.ReplacedByToken = replacedByToken;
        }

        public void BanAccount(int accountId, string origin)
        {
            var account = getAccount(accountId);

            // Update the database to mark the account as banned
            account.isBan = DateTime.UtcNow;
            _context.Accounts.Update(account);
            _context.SaveChanges();

            // Send the ban email
            SendBanEmail(account, origin);
        }

        public void WarningAccount(int accountId, string origin)
        {
            var account = getAccount(accountId);

            // Update the database to mark the account with a warning
            account.isWarning = DateTime.UtcNow;
            _context.Accounts.Update(account);
            _context.SaveChanges();

            // Send the warning email
            SendWarningEmail(account, origin);
        }

        private void sendVerificationEmail(Account account, string origin)
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectDirectory = Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
            var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "registration-code.html");
            var htmlContent = File.ReadAllText(htmlFilePath);
            htmlContent = htmlContent.Replace("{user-name}", account.FullName);
            htmlContent = htmlContent.Replace("{CODE}", account.VerificationToken);
            htmlContent = htmlContent.Replace("{link}", "FRONT END");
            htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

            _emailService.SendAsync(
                to: account.Email,
                subject: "QUIZLEARN - Registration",
                html: htmlContent
            );
        }

        private void sendAlreadyRegisteredEmail(string email, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
                message = $@"<p>If you don't know your password please visit the <a href=""{origin}/account/forgot-password"">forgot password</a> page.</p>";
            else
                message = "<p>If you don't know your password you can reset it via the <code>/accounts/forgot-password</code> api route.</p>";

            _emailService.SendAsync(
                to: email,
                subject: "QUIZLEARN - Email Already Registered",
                html: $@"<h4>Email Already Registered</h4>
                        <p>Your email <strong>{email}</strong> is already registered.</p>
                        {message}"
            );
        }

        private void sendPasswordResetEmail(Account account, string origin)
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectDirectory = Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
            var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "forgot-password.html");
            var htmlContent = File.ReadAllText(htmlFilePath);
            htmlContent = htmlContent.Replace("{user-name}", account.FullName);
            htmlContent = htmlContent.Replace("{CODE}", account.VerificationToken);
            htmlContent = htmlContent.Replace("{link}", "FRONT END");
            htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

            _emailService.SendAsync(
                to: account.Email,
                subject: "QUIZLEARN - Reset Password",
                html: htmlContent
            );
        }

        private void SendBanEmail(Account account, string origin)
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectDirectory = Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
            var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "ban.html");
            var htmlContent = File.ReadAllText(htmlFilePath);
            htmlContent = htmlContent.Replace("{user-name}", account.FullName);
            htmlContent = htmlContent.Replace("{link}", "FRONT END");
            htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

            _emailService.SendAsync(
                to: account.Email,
                subject: "QUIZLEARN - GOODBYE",
                html: htmlContent
            );
        }

        private void SendInviteEmail(Account from,Account to, string classroomName, string origin)
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectDirectory = Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
            var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "invitation.html");
            var htmlContent = File.ReadAllText(htmlFilePath);
            htmlContent = htmlContent.Replace("{user-email}", from.Email);
            htmlContent = htmlContent.Replace("{user-name}", to.FullName);
            htmlContent = htmlContent.Replace("{classroom-name}", classroomName);
            htmlContent = htmlContent.Replace("{link}", "FRONT END");
            htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

            _emailService.SendAsync(
                to: to.Email,
                subject: "QUIZLEARN - CLASSROOM INVITATION ",
                html: htmlContent
            );
        }

        private void SendWarningEmail(Account account, string origin)
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var projectDirectory = Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
            var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "waning.html");
            var htmlContent = File.ReadAllText(htmlFilePath);
            htmlContent = htmlContent.Replace("{user-name}", account.FullName);
            htmlContent = htmlContent.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
            htmlContent = htmlContent.Replace("{link}", "FRONT END");
            htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

            _emailService.SendAsync(
                to: account.Email,
                subject: "QUIZLEARN - VIOLATION RULES WARNING",
                html: htmlContent
            );
        }
    }
}
