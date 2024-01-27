using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class QuizBankController : BaseController
    {
        private readonly IQuizBankService _quizBankService;
        public QuizBankController(IQuizBankService quizBankService)
        {
            this._quizBankService = quizBankService;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<QuizBankResponse>> GetAll()
        {
            var result = _quizBankService.GetAll();
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
    }
}
