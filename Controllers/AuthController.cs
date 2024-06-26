﻿using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers;

[Authorize]
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
    public async Task<ActionResult<AuthenticateResponse>> AuthenticateAsync(AuthenticateRequest model)
    {
        var response = await _accountService.Authenticate(model, ipAddress());
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
    public async Task<ActionResult<AuthenticateResponse>> RefreshTokenAsync(string token)
    {
        var response = await _accountService.RefreshToken(token, ipAddress());
        /* setTokenCookie(response.RefreshToken);*/

        return Ok(new
        {
            JwtToken = response.AccessToken, response.RefreshToken
        });
    }


    [AllowAnonymous]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeTokenAsync(RevokeTokenRequest model)
    {
        // accept token from request body or cookie
        var token = model.Token ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Token is required" });

        // users can revoke their own tokens and admins can revoke any tokens
        if (!Account.OwnsToken(token) && Account.Role != Role.Admin)
            return Unauthorized(new { message = "Unauthorized" });

        await _accountService.RevokeToken(token, ipAddress());
        return Ok(new { message = "Token revoked" });
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest model)
    {
        await _accountService.Register(model);
        return Ok(new { message = "Registration successful, please check your email for verification instructions" });
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmailAsync(VerifyEmailRequest model)
    {
        await _accountService.VerifyEmail(model.Email, model.Token);
        return Ok(new { message = "Verification successful, you can now login" });
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync(ForgotPasswordRequest model)
    {
        var redirect = await _accountService.ForgotPassword(model, Request.Headers["origin"]);
        return Ok(new { message = "Please check your email for password reset instructions", data = redirect });
    }

    [AllowAnonymous]
    [HttpPost("validate-reset-token")]
    public async Task<IActionResult> VerifyResetPasswordPinAsync(VerifyResetAccountPinRequest model)
    {
        var requireAccount = await _accountService.GetByEmail(model.Email);

        var isVerified = _accountService.ValidateResetToken(model.Pin, requireAccount);
        var token = _accountService.IssueForgotPasswordToken(requireAccount);
        return isVerified
            ? Ok(new { message = "Success", data = new { token } })
            : BadRequest(new { message = "invalid-pin" });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync(ResetPasswordRequest model)
    {
        await _accountService.ResetPassword(model);
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