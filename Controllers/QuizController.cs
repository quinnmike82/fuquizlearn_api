﻿using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class QuizController : BaseController
    {
        private readonly IQuizService _quizService;
        public QuizController(IQuizService quizService)
        {
            this._quizService = quizService;
        }

        [Authorize]
        [HttpGet("{bankId:int}")]
        public ActionResult<IEnumerable<QuizResponse>> GetQuizFromBank(int bankId)
        {
            var result = _quizService.GetAllQuizFromBank(bankId, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{bankId:int}")]
        public ActionResult<QuizResponse> AddQuizInBank(int bankId, QuizCreate model)
        {
            var result = _quizService.AddQuizInBank(Account, model, bankId);
            return Ok(result);
        }
        
        [Authorize]
        [HttpPut("{bankId:int}/{quizId:int}")]
        public ActionResult<QuizResponse> UpdateQuizInBank(int bankId,int quizId, QuizUpdate model)
        {
            var result = _quizService.UpdateQuizInBank(bankId, quizId, model, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{bankId:int}/{quizId:int}")]
        public IActionResult DeleteQuizInBank(int bankId,int quizId)
        {
            _quizService.DeleteQuizInBank(bankId, quizId, Account);
            return Ok(new { message = "Quiz deleted successfully" });
        }
    }
}