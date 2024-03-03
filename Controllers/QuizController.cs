using fuquizlearn_api.Authorization;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
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
        private readonly IGeminiAIService _geminiAIService;
        public QuizController(IQuizService quizService, IGeminiAIService geminiAIService)
        {
            this._quizService = quizService;
            this._geminiAIService = geminiAIService;
        }

        [AllowAnonymous]
        [HttpGet("{bankId:int}")]
        public async Task<ActionResult<PagedResponse<QuizResponse>>> GetQuizFromBank(int bankId, [FromQuery] QuizPagedRequest options)
        {
            var result = await _quizService.GetAllQuizFromBank(bankId, Account, options);
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
        
        [HttpPost("text-only-input")]
        public async Task<ActionResult<IEnumerable<QuizResponse>>> GetTextResult(QuizCreate prompt)
        {
            var result = await _geminiAIService.GetTextOnly(prompt);
            return Ok(result);
        }
        
        [HttpPost("text-image-input")]
        public ActionResult<QuizResponse> GetTextPictureResult(Stream file, string prompt)
        {
            var result = _geminiAIService.GetTextAndImage(file, prompt);
            return Ok(result);
        }
    }
}
