using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace fuquizlearn_api.Controllers;

[Authorize]
[ApiController]
public class AccountsController : BaseController
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAllAsync([FromQuery] PagedRequest options)
    {
        var accounts = await _accountService.GetAll(options);
        return Ok(accounts);
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetbanAccount([FromQuery] PagedRequest options)
    {
        var accounts = await _accountService.GetBannedAccount(options);
        return Ok(accounts);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public ActionResult<AccountResponse> GetById(int id)
    {
        // users can get their own account and admins can get any account
        /*            if (id != Account.Id && Account.Role != Role.Admin)
                        return Unauthorized(new { message = "Unauthorized" });*/

        var account = _accountService.GetById(id);
        return Ok(account);
    }

    [AllowAnonymous]
    [HttpPost]
    public ActionResult<AccountResponse> Create(CreateRequest model)
    {
        var account = _accountService.Create(model);
        return Ok(account);
    }

    [AllowAnonymous]
    [HttpPut("{id:int}")]
    public ActionResult<AccountResponse> Update(int id, UpdateRequest model)
    {
        // users can update their own account and admins can update any account
        /*            if (id != Account.Id && Account.Role != Role.Admin)
                        return Unauthorized(new { message = "Unauthorized" });*/

        // only admins can update role
        if (Account.Role != Role.Admin)
            model.Role = null;

        var account = _accountService.Update(id, model);
        return Ok(account);
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<ActionResult<AccountResponse>> ChangePassword(ChangePassRequest model)
    {
        var account = await _accountService.ChangePassword(model, Account);
        return Ok(account);
    }

    [AllowAnonymous]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        // users can delete their own account and admins can delete any account
        /*            if (id != Account.Id && Account.Role != Role.Admin)
                        return Unauthorized(new { message = "Unauthorized" });*/

        _accountService.Delete(id);
        return Ok(new { message = "Account deleted successfully" });
    }

    [AllowAnonymous]
    [HttpPost("ban/{id:int}")]
    public IActionResult BanAccount(int id)
    {
        try
        {
            _accountService.BanAccount(id, Request.Headers["origin"]);
            return Ok(new { message = "Account banned successfully" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [AllowAnonymous]
    [HttpPost("unban/{id:int}")]
    public IActionResult UnbanAccount(int id)
    {
        try
        {
            _accountService.UnbanAccount(id, Request.Headers["origin"]);
            return Ok(new { message = "Account unbanned successfully" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("warning/{id:int}")]
    public IActionResult WarningAccount(int id)
    {
        try
        {
            _accountService.WarningAccount(id, Request.Headers["origin"]);
            return Ok(new { message = "Account warned successfully" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("profile")]
    public IActionResult GetUserProfile()
    {
        try
        {
            var id = Account.Id;
            var user = _accountService.GetById(id);

            return Ok(user);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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