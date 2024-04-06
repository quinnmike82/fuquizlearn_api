using fuquizlearn_api.Helpers;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace fuquizlearn_api.Authorization
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppSettings _appSettings;

        public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
        {
            _next = next;
            _appSettings = appSettings.Value;
        }

        public async Task Invoke(HttpContext context, DataContext dataContext, IJwtUtils jwtUtils)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var accountId = jwtUtils.ValidateJwtToken(token);
            if (accountId != null)
            {
                // attach account to context on successful jwt validation
                var account = await dataContext.Accounts.FindAsync(accountId.Value);
                context.Items["Account"] = account;
                if (context.Request.Path.Value.Contains("gameSocket"))
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity( new[] 
                        {
                            new Claim("email", account.Email)
                        }
                        ));
                }
            }

            await _next(context);
        }
    }
}
