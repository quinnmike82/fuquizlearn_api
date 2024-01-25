using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers;

[Authorize]
[Route("auth")]
[ApiController]
public class AuthController : BaseController
{
    private readonly IAccountService _accountService;

    public AuthController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
    {
        var response = _accountService.Authenticate(model, ipAddress());
        setTokenCookie(response.RefreshToken.token);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login/google")]
    public async Task<ActionResult<AuthenticateResponse>> GoogleAuthenticateAsync(LoginGoogleRequest model)
    {
        var response = await _accountService.GoogleAuthenticate(model, ipAddress());
        setTokenCookie(response.RefreshToken.token);
        return Ok(response);
    }


    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public ActionResult<AuthenticateResponse> RefreshToken(string token)
    {
        var response = _accountService.RefreshToken(token, ipAddress());
        /* setTokenCookie(response.RefreshToken);*/

        return Ok(new
        {
            JwtToken = response.AccessToken, response.RefreshToken
        });
    }


    [AllowAnonymous]
    [HttpPost("revoke-token")]
    public IActionResult RevokeToken(RevokeTokenRequest model)
    {
        // accept token from request body or cookie
        var token = model.Token ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Token is required" });

        // users can revoke their own tokens and admins can revoke any tokens
        if (!Account.OwnsToken(token) && Account.Role != Role.Admin)
            return Unauthorized(new { message = "Unauthorized" });

        _accountService.RevokeToken(token, ipAddress());
        return Ok(new { message = "Token revoked" });
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public IActionResult Register([FromForm] RegisterRequest model)
    {
        _accountService.Register(model, Request.Headers["origin"]);
        return Ok(new { message = "Registration successful, please check your email for verification instructions" });
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public IActionResult VerifyEmail(VerifyEmailRequest model)
    {
        _accountService.VerifyEmail(model.Token);
        return Ok(new { message = "Verification successful, you can now login" });
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword(ForgotPasswordRequest model)
    {
        var redirect = _accountService.ForgotPassword(model, Request.Headers["origin"]);
        return Ok(new { message = "Please check your email for password reset instructions", data = redirect });
    }

    [AllowAnonymous]
    [HttpPost("validate-reset-token")]
    public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
    {
        _accountService.ValidateResetToken(model);
        return Ok(new { message = "Token is valid" });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public IActionResult ResetPassword(ResetPasswordRequest model)
    {
        _accountService.ResetPassword(model);
        return Ok(new { message = "Password reset successful, you can now login" });
    }

    // helper methods

    private void setTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    private string ipAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"];
        return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
    }
}