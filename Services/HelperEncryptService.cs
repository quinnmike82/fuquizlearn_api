using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace fuquizlearn_api.Services
{
    public interface IHelperEncryptService
    {
        string JwtEncrypt(Account payload, JwtPayload? options = null);
        JwtSecurityToken? JwtDecrypt(string token);
        bool JwtVerify(string? token);

        RefreshToken IssuesRefreshToken(string ipAddress);
    }
    public class HelperEncryptService : IHelperEncryptService
    {
        private readonly AppSettings _appSettings;
        private readonly string _accessTokenSecret;
        private readonly int _accessTokenExpires;
        private readonly int _refreshTokenExpires;
        private readonly JwtSecurityTokenHandler jwtSecurityTokenHandler;

        public HelperEncryptService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;

            _accessTokenSecret = _appSettings.AccessTokenSecret;
            _accessTokenExpires = _appSettings.AccessTokenTTL;
            _refreshTokenExpires = _appSettings.RefreshTokenTTL;

            jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        }

        public JwtSecurityToken? JwtDecrypt(string token)
        {
            var key = Encoding.ASCII.GetBytes(_accessTokenSecret);

            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            jwtSecurityTokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;

            return jwtToken;
        }

        public string JwtEncrypt(Account account, JwtPayload? options = null)
        {
            var key = Encoding.ASCII.GetBytes(_accessTokenSecret);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddDays(_accessTokenExpires),
                IssuedAt = options?.IssuedAt,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtSecurityTokenHandler.CreateToken(tokenDescriptor);
            return jwtSecurityTokenHandler.WriteToken(token);
        }

        public bool JwtVerify(string? token)
        {
            if (token.IsNullOrEmpty())
            {
                return false;
            }

            var key = Encoding.ASCII.GetBytes(_accessTokenSecret);

            try
            {
                jwtSecurityTokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                }, out SecurityToken validatedToken);
                return true;
            }
            catch
            {
                return false;
            }
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
                CreatedByIp = ipAddress,
            };

            return refreshToken;
        }
    }
}
