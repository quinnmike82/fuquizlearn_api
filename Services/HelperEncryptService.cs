using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace fuquizlearn_api.Services;

public interface IHelperEncryptService
{
    string AccessTokenEncrypt(Account payload, JwtPayload? options = null);
    JwtSecurityToken? AccessTokenDecrypt(string token);
    bool AccessTokenVerify(string? token);

    string JwtEncrypt(string key, ClaimsIdentity subject,
        long exp);

    JwtSecurityToken JwtDecrypt(string key, string token);
    RefreshToken IssuesRefreshToken(string ipAddress);
}

public class HelperEncryptService : IHelperEncryptService
{
    private readonly int _accessTokenExpires;
    private readonly string _accessTokenSecret;
    private readonly AppSettings _appSettings;
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
    private readonly int _refreshTokenExpires;

    public HelperEncryptService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;

        _accessTokenSecret = _appSettings.AccessTokenSecret;
        _accessTokenExpires = _appSettings.AccessTokenTTL;
        _refreshTokenExpires = _appSettings.RefreshTokenTTL;

        _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
    }

    public JwtSecurityToken? AccessTokenDecrypt(string token)
    {
        var key = Encoding.ASCII.GetBytes(_accessTokenSecret);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        _jwtSecurityTokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
        var jwtToken = validatedToken as JwtSecurityToken;

        return jwtToken;
    }

    public string AccessTokenEncrypt(Account account, JwtPayload? options = null)
    {
        var key = Encoding.ASCII.GetBytes(_accessTokenSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
            Expires = DateTime.UtcNow.AddDays(_accessTokenExpires),
            IssuedAt = options?.IssuedAt,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = _jwtSecurityTokenHandler.CreateToken(tokenDescriptor);
        return _jwtSecurityTokenHandler.WriteToken(token);
    }

    public bool AccessTokenVerify(string? token)
    {
        if (token.IsNullOrEmpty()) return false;

        var key = Encoding.ASCII.GetBytes(_accessTokenSecret);

        try
        {
            _jwtSecurityTokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            }, out var validatedToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public JwtSecurityToken JwtDecrypt(string key, string token)
    {
        var keyByte = Encoding.ASCII.GetBytes(key);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyByte),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        _jwtSecurityTokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
        var jwtToken = validatedToken as JwtSecurityToken;

        return jwtToken;
    }

    public RefreshToken IssuesRefreshToken(string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            // token is a cryptographically strong random sequence of values
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64)),
            // token is valid for 7 days
            Expires = DateTime.UtcNow.AddDays(_refreshTokenExpires),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        return refreshToken;
    }

    public string JwtEncrypt(string key, ClaimsIdentity subject, long exp)
    {
        var tokenKey = Encoding.ASCII.GetBytes(key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = subject,
            Expires = DateTime.UtcNow.AddTicks(exp),
            IssuedAt = DateTime.Now,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = _jwtSecurityTokenHandler.CreateToken(tokenDescriptor);
        return _jwtSecurityTokenHandler.WriteToken(token);
    }
}