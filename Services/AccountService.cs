﻿using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using AutoMapper;
using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BC = BCrypt.Net.BCrypt;

namespace fuquizlearn_api.Services;

public interface IAccountService
{
    Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress);
    Task<AuthenticateResponse> GoogleAuthenticate(LoginGoogleRequest model, string ipAddress);
    Task<AuthenticateResponse> RefreshToken(string token, string ipAddress);
    Task RevokeToken(string token, string ipAddress);
    string IssueForgotPasswordToken(Account account);
    bool ValidateResetToken(string token, Account account);
    Task Register(RegisterRequest model);
    Task VerifyEmail(string email, string token);
    Task<string> ForgotPassword(ForgotPasswordRequest model, string origin);
    Task ResetPassword(ResetPasswordRequest model);
    Task<Account> GetByEmail(string email);
    Task<PagedResponse<AdminAccountResponse>> GetAll(PagedRequest options);
    Task<AccountResponse> GetById(int id);
    Task<AccountResponse> Create(CreateRequest model);
    Task<AccountResponse> Update(int id, UpdateRequest model);
    Task Delete(int id);
    Task BanAccount(int id, string origin, Account account);
    Task UnbanAccount(int id, string origin, Account account);
    Task WarningAccount(int id, string origin, Account account);
    Task WarningAccount(int id, string origin);


    Task<PagedResponse<AdminAccountResponse>> GetBannedAccount(PagedRequest options);

    Task<AccountResponse> ChangePassword(ChangePassRequest model, Account account);

}

public class AccountService : IAccountService
{
    private readonly AppSettings _appSettings;
    private readonly DataContext _context;
    private readonly IEmailService _emailService;
    private readonly IGoogleService _googleService;
    private readonly IHelperCryptoService _helperCryptoService;
    private readonly IHelperDateService _helperDateService;
    private readonly IHelperEncryptService _helperEncryptService;
    private readonly IHelperFrontEnd _helperFrontEnd;
    private readonly IJwtUtils _jwtUtils;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public AccountService(
        DataContext context,
        IJwtUtils jwtUtils,
        IMapper mapper,
        IOptions<AppSettings> appSettings,
        IEmailService emailService,
        IHelperDateService helperDateService,
        IHelperEncryptService helperEncryptService,
        IHelperCryptoService helperCryptoService,
        IHelperFrontEnd helperFrontEnd,
        IGoogleService googleService,
        INotificationService notificationService)
    {
        _context = context;
        _jwtUtils = jwtUtils;
        _mapper = mapper;
        _appSettings = appSettings.Value;
        _emailService = emailService;
        _helperEncryptService = helperEncryptService;
        _helperDateService = helperDateService;
        _helperFrontEnd = helperFrontEnd;
        _googleService = googleService;
        _helperCryptoService = helperCryptoService;
        _notificationService = notificationService;
    }

    public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress)
    {
        var account =
            await _context.Accounts.FirstOrDefaultAsync(x =>
                x.Email == model.EmailOrUsername || x.Username == model.EmailOrUsername);

        // validate
        if (account == null || !BC.Verify(model.Password, account.PasswordHash))
            throw new AppException("Login.WrongEmailPassword");
        if (account.isBan != null)
        {
            throw new AppException("Login.wasBaned");
        }
        // authentication successful so generate jwt and refresh tokens
        var jwtToken = _helperEncryptService.AccessTokenEncrypt(account);
        var jwtExpires =
            _helperDateService.ConvertToUnixTimestamp(_helperEncryptService.AccessTokenDecrypt(jwtToken).ValidTo);
        var refreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
        var refreshTokenExpires = _helperDateService.ConvertToUnixTimestamp(refreshToken.Expires);

        if (refreshToken == null || jwtToken == null) throw new AppException("Token.Issue");

        account.RefreshTokens.Add(refreshToken);

        // remove old refresh tokens from account
        removeOldRefreshTokens(account);

        // save changes to db
        _context.Update(account);
        await _context.SaveChangesAsync();

        var response = _mapper.Map<AuthenticateResponse>(account);
        response.AccessToken = new Token
        {
            token = jwtToken,
            expiredAt = jwtExpires
        };
        response.RefreshToken = new Token
        {
            token = refreshToken.Token,
            expiredAt = refreshTokenExpires
        };
        return response;
    }

    public async Task<AuthenticateResponse> GoogleAuthenticate(LoginGoogleRequest model, string ipAddress)
    {
        var email = await _googleService.GetEmailByToken(model.Token) ?? throw new AppException("Validations.errors.signup.email");
        var account = await GetByEmail(email) ?? throw new AppException("ForgotPassword.errors.account-fail");

        // authentication successful so generate jwt and refresh tokens
        var jwtToken = _helperEncryptService.AccessTokenEncrypt(account);
        var jwtExpires =
            _helperDateService.ConvertToUnixTimestamp(_helperEncryptService.AccessTokenDecrypt(jwtToken).ValidTo);
        var refreshToken = _jwtUtils.GenerateRefreshToken(ipAddress);
        var refreshTokenExpires = _helperDateService.ConvertToUnixTimestamp(refreshToken.Expires);

        if (refreshToken == null || jwtToken == null) throw new AppException("Token.Issue");

        account.RefreshTokens.Add(refreshToken);

        // remove old refresh tokens from account
        removeOldRefreshTokens(account);

        // save changes to db
        _context.Update(account);
        await _context.SaveChangesAsync();

        var response = _mapper.Map<AuthenticateResponse>(account);
        response.AccessToken = new Token
        {
            token = jwtToken,
            expiredAt = jwtExpires
        };
        response.RefreshToken = new Token
        {
            token = refreshToken.Token,
            expiredAt = refreshTokenExpires
        };
        return response;
    }

    public async Task<AuthenticateResponse> RefreshToken(string token, string ipAddress)
    {
        var account = await getAccountByRefreshToken(token);
        var refreshToken = account.RefreshTokens.Single(x => x.Token == token);

        if (refreshToken.IsRevoked)
        {
            // revoke all descendant tokens in case this token has been compromised
            revokeDescendantRefreshTokens(refreshToken, account, ipAddress,
                $"Attempted reuse of revoked ancestor token: {token}");
            _context.Update(account);
            _context.SaveChanges();
        }

        if (!refreshToken.IsActive)
            throw new AppException("Token.Invalid");

        // replace old refresh token with a new one (rotate token)
        var newRefreshToken = rotateRefreshToken(refreshToken, ipAddress);
        account.RefreshTokens.Add(newRefreshToken);


        // remove old refresh tokens from account
        removeOldRefreshTokens(account);

        // save changes to db
        _context.Update(account);
        await _context.SaveChangesAsync();

        // generate new jwt
        var jwtToken = _helperEncryptService.AccessTokenEncrypt(account);
        var jwtExpires =
            _helperDateService.ConvertToUnixTimestamp(_helperEncryptService.AccessTokenDecrypt(jwtToken).ValidTo);
        var refreshTokenExpires = _helperDateService.ConvertToUnixTimestamp(refreshToken.Expires);

        // return data in authenticate response object
        var response = _mapper.Map<AuthenticateResponse>(account);
        response.AccessToken = new Token
        {
            token = jwtToken,
            expiredAt = jwtExpires
        };
        response.RefreshToken = new Token
        {
            token = refreshToken.Token,
            expiredAt = refreshTokenExpires
        };
        return response;
    }

    public async Task RevokeToken(string token, string ipAddress)
    {
        var account = await getAccountByRefreshToken(token);
        var refreshToken = account.RefreshTokens.Single(x => x.Token == token);

        if (!refreshToken.IsActive)
            throw new AppException("Token.Invalid");

        // revoke token and save
        revokeRefreshToken(refreshToken, ipAddress, "Revoked without replacement");
        _context.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task VerifyEmail(string email, string token)
    {
        var account = _context.Accounts.SingleOrDefault(x => x.VerificationToken == token && x.Email == email);

        if (account == null)
            throw new AppException("verification-failed");

        account.Verified = DateTime.UtcNow;
        account.VerificationToken = null;

        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public string IssueForgotPasswordToken(Account account)
    {
        // create reset token that expires after 1 day

        var claims = new ClaimsIdentity(new[
        ]
        {
            new Claim("email", account.Email)
        });
        var token = _helperEncryptService.JwtEncrypt(_appSettings.ForgotPasswordSecret, claims, TimeSpan.TicksPerDay);
        return token;
    }

    public async Task<string> ForgotPassword(ForgotPasswordRequest model, string origin)
    {
        var account = await _context.Accounts.SingleOrDefaultAsync(x => x.Email == model.Email);

        // always return ok response to prevent email enumeration
        if (account == null) throw new AppException("ForgotPassword.errors.account-fail");
        var generatePin = _helperCryptoService.GeneratePIN(6);
        var hashedPin = _helperCryptoService.HashSHA256(generatePin);
        var token = IssueForgotPasswordToken(account);
        account.ResetToken = hashedPin;
        account.ResetTokenExpires = DateTime.UtcNow.AddDays(1);
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
        var redirect = _helperFrontEnd.GetUrl("/forgot/verify?t=" + token);
        // send email
        sendPasswordResetEmail(account, generatePin, redirect);
        return redirect;
    }

    public bool ValidateResetToken(string token, Account account)
    {
        var isTokenMatch = _helperCryptoService.CompareSHA256(account.ResetToken, token);

        return isTokenMatch;
    }

    public async Task ResetPassword(ResetPasswordRequest model)
    {
        var claims = _helperEncryptService.JwtDecrypt(_appSettings.ForgotPasswordSecret, model.Token);
        var email = claims.Claims.First(claim => claim.Type == "email").Value;
        var account = await _context.Accounts.FirstOrDefaultAsync(account => account.Email == email);
        if (account == null) throw new AppException("ForgotPassword.errors.account-fail");
        if (account.ResetToken == null) throw new AppException("ForgotPassword.errors.account-fail");
        // update password and remove reset token
        account.PasswordHash = BC.HashPassword(model.Password);
        account.PasswordReset = DateTime.UtcNow;
        account.ResetToken = null;
        account.ResetTokenExpires = null;

        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResponse<AdminAccountResponse>> GetAll(PagedRequest options)
    {
        var accounts = await _context.Accounts.ToPagedAsync(options,
            x => x.FullName.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
        return new PagedResponse<AdminAccountResponse>
        {
            Data = _mapper.Map<IEnumerable<AdminAccountResponse>>(accounts.Data),
            Metadata = accounts.Metadata
        };
    }

    public async Task<AccountResponse> GetById(int id)
    {
        var account = await getAccount(id);
        return _mapper.Map<AccountResponse>(account);
    }

    public async Task<AccountResponse> Create(CreateRequest model)
    {
        // validate
        if (await _context.Accounts.AnyAsync(x => x.Email == model.Email))
            throw new AppException($"email-existed");
        if (await _context.Accounts.AnyAsync(x => x.Username == model.Username))
            throw new AppException($"username-existed");

        // map model to new account object
        var account = _mapper.Map<Account>(model);
        account.Created = DateTime.UtcNow;
        account.Verified = DateTime.UtcNow;

        // hash password
        account.PasswordHash = BC.HashPassword(model.Password);

        // save account
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return _mapper.Map<AccountResponse>(account);
    }

    public async Task<AccountResponse> Update(int id, UpdateRequest model)
    {
        var account = await getAccount(id);

        // validate
        if (account.Email != model.Email && _context.Accounts.Any(x => x.Email == model.Email))
            throw new AppException($"email-existed");

        // hash password if it was entered
        if (!string.IsNullOrEmpty(model.Password))
            account.PasswordHash = BC.HashPassword(model.Password);

        // copy model to account and save
        _mapper.Map(model, account);
        account.Updated = DateTime.UtcNow;
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();

        return _mapper.Map<AccountResponse>(account);
    }

    public async Task Delete(int id)
    {
        var account = await getAccount(id);
        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
    }

    public async Task BanAccount(int accountId, string origin, Account ad)
    {
        if (ad.Role != Role.Admin)
            throw new UnauthorizedAccessException();
        var account =await getAccount(accountId);

        // Update the database to mark the account as banned
        account.isBan = DateTime.UtcNow;
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();

        // Send the ban email
        SendBanEmail(account, origin);
    }

    public async Task WarningAccount(int accountId, string origin, Account ad)
    {
        if (ad.Role != Role.Admin)
            throw new UnauthorizedAccessException();
        var account = await getAccount(accountId);

        // Update the database to mark the account with a warning
        account.isWarning = DateTime.UtcNow;
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
        await _notificationService.NotificationTrigger(new List<int> { accountId }, "Warning", "reported", string.Empty);
        // Send the warning email
        SendWarningEmail(account, origin);
    }
    public async Task WarningAccount(int accountId, string origin)
    {
        var account = await getAccount(accountId);

        // Update the database to mark the account with a warning
        account.isWarning = DateTime.UtcNow;
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
        await _notificationService.NotificationTrigger(new List<int> { accountId }, "Warning", "reported", string.Empty);
        // Send the warning email
        SendWarningEmail(account, origin);
    }

    public async Task<Account> GetByEmail(string email)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == email);
        return account ?? throw new KeyNotFoundException("ForgotPassword.errors.account-fail");
    }

    public async Task Register(RegisterRequest model)
    {
        // validate
        var checkEmail = await _context.Accounts.AnyAsync(x => x.Email.Equals(model.Email));
        if (checkEmail) throw new AppException("email-existed");
        var checkUser = await _context.Accounts.AnyAsync(x => x.Username == model.Username);
        if (checkUser) throw new AppException("username-existed");

        // map model to new account object
        var account = _mapper.Map<Account>(model);

        account.Role = Role.User;
        account.Created = DateTime.UtcNow;
        account.VerificationToken = _helperCryptoService.GeneratePIN(6);

        // hash password
        account.PasswordHash = BC.HashPassword(model.Password);

        // save account
        _context.Accounts.Add(account);
        _context.SaveChanges();
        Console.WriteLine(account.VerificationToken);
        // send email
        sendVerificationEmail(account, _helperFrontEnd.GetUrl("/signup/verify?email=" + HttpUtility.UrlEncode(account.Email)));
    }

    // helper methods

    private async Task<Account> getAccount(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null) throw new KeyNotFoundException("ForgotPassword.errors.account-fail");
        return account;
    }

    private async Task<Account> getAccountByRefreshToken(string token)
    {
        var account = await _context.Accounts.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));
        if (account == null) throw new AppException("Token.Invalid");
        return account;
    }

    private async Task<Account> getAccountByResetToken(string token)
    {
        var account = await _context.Accounts.SingleOrDefaultAsync(x =>
            x.ResetToken == token && x.ResetTokenExpires > DateTime.UtcNow);
        if (account == null) throw new AppException("Token.Invalid");
        return account;
    }

    private string generateJwtToken(Account account)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_appSettings.AccessTokenSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
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

    private void revokeDescendantRefreshTokens(RefreshToken refreshToken, Account account, string ipAddress,
        string reason)
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

    private void revokeRefreshToken(RefreshToken token, string ipAddress, string reason = null,
        string replacedByToken = null)
    {
        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason;
        token.ReplacedByToken = replacedByToken;
    }

    private void sendVerificationEmail(Account account, string origin)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var projectDirectory =
            Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
        var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "registration-code.html");
        var htmlContent = File.ReadAllText(htmlFilePath);
        htmlContent = htmlContent.Replace("{user-name}", account.FullName);
        htmlContent = htmlContent.Replace("{CODE}", account.VerificationToken);
        htmlContent = htmlContent.Replace("{link}", origin);
        htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

        _emailService.SendAsync(
            account.Email,
            "QUIZLEARN - Registration",
            htmlContent
        );
    }

    private void sendAlreadyRegisteredEmail(string email, string origin)
    {
        string message;
        if (!string.IsNullOrEmpty(origin))
            message =
                $@"<p>If you don't know your password please visit the <a href=""{origin}/account/forgot-password"">forgot password</a> page.</p>";
        else
            message =
                "<p>If you don't know your password you can reset it via the <code>/accounts/forgot-password</code> api route.</p>";

        _emailService.SendAsync(
            email,
            "QUIZLEARN - Email Already Registered",
            $@"<h4>Email Already Registered</h4>
                        <p>Your email <strong>{email}</strong> is already registered.</p>
                        {message}"
        );
    }

    private void sendPasswordResetEmail(Account account, string code, string origin)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var projectDirectory =
            Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
        var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "forgot-password.html");
        var htmlContent = File.ReadAllText(htmlFilePath);
        htmlContent = htmlContent.Replace("{user-name}", account.FullName);
        htmlContent = htmlContent.Replace("{CODE}", code);
        htmlContent = htmlContent.Replace("{link}", origin);
        htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

        _emailService.SendAsync(
            account.Email,
            "QUIZLEARN - Reset Password",
            htmlContent
        );
    }

    private void SendBanEmail(Account account, string origin)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var projectDirectory =
            Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
        var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "ban.html");
        var htmlContent = File.ReadAllText(htmlFilePath);
        htmlContent = htmlContent.Replace("{user-name}", account.FullName);
        htmlContent = htmlContent.Replace("{link}", "FRONT END");
        htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

        _emailService.SendAsync(
            account.Email,
            "QUIZLEARN - GOODBYE",
            htmlContent
        );
    }
    private void SendUnbanEmail(Account account, string origin)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var projectDirectory =
            Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
        var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "unban.html");
        var htmlContent = File.ReadAllText(htmlFilePath);
        htmlContent = htmlContent.Replace("{user-name}", account.FullName);
        htmlContent = htmlContent.Replace("{link}", "FRONT END");
        htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

        _emailService.SendAsync(
            account.Email,
            "QUIZLEARN - GOODBYE",
            htmlContent
        );
    }

    private void SendInviteEmail(Account from, Account to, string classroomName, string origin)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var projectDirectory =
            Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
        var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "invitation.html");
        var htmlContent = File.ReadAllText(htmlFilePath);
        htmlContent = htmlContent.Replace("{user-email}", from.Email);
        htmlContent = htmlContent.Replace("{user-name}", to.FullName);
        htmlContent = htmlContent.Replace("{classroom-name}", classroomName);
        htmlContent = htmlContent.Replace("{link}", "FRONT END");
        htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

        _emailService.SendAsync(
            to.Email,
            "QUIZLEARN - CLASSROOM INVITATION ",
            htmlContent
        );
    }

    private void SendWarningEmail(Account account, string origin)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var projectDirectory =
            Path.Combine(assemblyDirectory, "..", "..", ".."); // Go up three levels from AccountService.cs
        var htmlFilePath = Path.Combine(projectDirectory, "EmailTemplate", "waning.html");
        var htmlContent = File.ReadAllText(htmlFilePath);
        htmlContent = htmlContent.Replace("{user-name}", account.FullName);
        htmlContent = htmlContent.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
        htmlContent = htmlContent.Replace("{link}", "FRONT END");
        htmlContent = htmlContent.Replace("{mail}", "ngocvlqt1995@gmail.com");

        _emailService.SendAsync(
            account.Email,
            "QUIZLEARN - VIOLATION RULES WARNING",
            htmlContent
        );
    }

    public async Task<AccountResponse> ChangePassword(ChangePassRequest model, Account account)
    {
        var entity = await _context.Accounts.FirstOrDefaultAsync(acc => acc.Id == account.Id);
        if (entity == null)
        {
            throw new KeyNotFoundException("ForgotPassword.errors.account-fail");
        }
        if(!BC.Verify(model.OldPassword, entity.PasswordHash))
        {
            throw new AppException("ChangePassword.WrongOldPassword");
        }
        entity.PasswordHash = BC.HashPassword(model.Password);
        _context.Accounts.Update(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<AccountResponse>(entity);
    }

    public async Task UnbanAccount(int id, string origin, Account ad)
    {
        if (ad.Role != Role.Admin)
            throw new UnauthorizedAccessException();
        var account = await getAccount(id);

        // Update the database to mark the account as banned
        account.isBan = null;
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();

        // Send the ban email
        SendUnbanEmail(account, origin);
    }

    public async Task<PagedResponse<AdminAccountResponse>> GetBannedAccount(PagedRequest options)
    {
        var accounts = await _context.Accounts.Where(i => i.isBan != null).ToPagedAsync(options,
   q => q.Username.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
        return new PagedResponse<AdminAccountResponse>
        {
            Data = _mapper.Map<IEnumerable<AdminAccountResponse>>(accounts.Data),
            Metadata = accounts.Metadata
        };
    }
}