﻿using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers;

[Authorize]
[ApiController]
public class QuizBankController : BaseController
{
    private readonly IQuizBankService _quizBankService;

    public QuizBankController(IQuizBankService quizBankService)
    {
        _quizBankService = quizBankService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<QuizBankResponse>>> GetAll([FromQuery] PagedRequest options)
    {
        var result = await _quizBankService.GetAll(options);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("Get-by-subject")]
    public async Task<ActionResult<PagedResponse<QuizBankResponse>>> GetBySubject([FromQuery] string tag, [FromQuery] PagedRequest options)
    {
        var result = await _quizBankService.GetBySubject(options, tag, Account);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("GetMyQuizBank")]
    public async Task<ActionResult<PagedResponse<QuizBankResponse>>> GetMy([FromQuery] PagedRequest options)
    {
        var result = await _quizBankService.GetMy(options, Account);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public ActionResult<QuizBankResponse> GetById(int id)
    {
        var result = _quizBankService.GetById(id);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    public ActionResult<QuizBankResponse> Create(QuizBankCreate model)
    {
        var result = _quizBankService.Create(Account, model);
        return Ok(result);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public ActionResult<QuizBankResponse> Update(int id, QuizBankUpdate model)
    {
        var account = _quizBankService.Update(id, model, Account);
        return Ok(account);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        _quizBankService.Delete(id, Account);
        return Ok(new { message = "QuizBank deleted successfully" });
    }

    [Authorize]
    [HttpPost("rating/{id:int}")]
    public ActionResult<QuizBankResponse> Rating(int id, [FromQuery] RatingRequest rating)
    {
        var result = _quizBankService.Rating(id, Account, rating.Star);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("related/{id:int}")]
    public ActionResult<IEnumerable<QuizBankResponse>> GetRelated(int id)
    {
        var result = _quizBankService.GetRelated(id);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("saveprogress/{quizbankId:int}")]
    public async Task<ActionResult<ProgressResponse>> SaveProgress(int quizbankId, [FromBody] SaveProgressRequest saveProgressRequest)
    {
        var result = await _quizBankService.SaveProgress(quizbankId, Account, saveProgressRequest);
        return Ok(result);
    }
    
    [Authorize]
    [HttpGet("getprogress/{quizbankId:int}")]
    public async Task<ActionResult<ProgressResponse>> GetProgress(int quizbankId)
    {
        var result = await _quizBankService.GetProgress(quizbankId, Account);
        return Ok(result);
    }

    [HttpPost("copyquizbank/{quizbankId}")]
    [Authorize]
    public async Task<ActionResult<QuizBankResponse>> CopyQuizBank([FromBody] QuizBankUpdate quizBankUpdate, int quizbankId)
    {
        var newName = quizBankUpdate.BankName ?? "";
        var result = await _quizBankService.CopyQuizBank(newName, quizbankId, Account);
        return Ok(result);
    }
}