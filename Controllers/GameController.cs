using fuquizlearn_api.Authorization;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [ApiController]
    public class GameController : BaseController
    {
        private readonly IGameService _gameService;
        public GameController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [Authorize]
        [HttpPost()]
        public async Task<ActionResult<GameResponse>> Create([FromQuery] GameCreate gameCreate)
        {
            var result = await _gameService.CreateGame(gameCreate, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("join-game/{gameId}")]
        public async Task<IActionResult> Join(int gameId)
        {
            await _gameService.Join(gameId, Account);
            return Ok();
        }

        [Authorize]
        [HttpGet("get-all-by-classroom/{classroomId}")]
        public async Task<ActionResult<PagedResponse<GameResponse>>> GetAllByClassId(int classroomId, [FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetAllByClassId(classroomId, option, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("get-all-by-quizbank/{quizbankId}")]
        public async Task<ActionResult<PagedResponse<GameResponse>>> GetAllByQuizBankId(int quizbankId, [FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetAllByQuizBankId(quizbankId, option, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("get-my-joined/")]
        public async Task<ActionResult<PagedResponse<GameResponse>>> GetMyJoined([FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetMyJoined(option, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{gameId}")]
        public async Task<ActionResult<GameResponse>> GetById(int gameId)
        {
            var result = await _gameService.GetById(gameId, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("get-quizes-in-game/{gameId}")]
        public async Task<ActionResult<GameResponse>> GetQuizes(int gameId,[FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetQuizes(gameId, option, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("start/{gameId}")]
        public async Task<ActionResult<GameResponse>> StartById(int gameId)
        {
            var result = await _gameService.StartById(gameId, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("end/{gameId}")]
        public async Task<ActionResult<GameResponse>> EndById(int gameId)
        {
            var result = await _gameService.EndById(gameId, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("add-answer-history/{gameId}")]
        public async Task<ActionResult<GameRecordResponse>> AddAnswerHistory(int gameId, AnswerHistoryRequest answerHistoryRequest)
        {
            var result = await _gameService.AddAnswerHistory(gameId, answerHistoryRequest, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("my-record-in-game/{gameId}")]
        public async Task<ActionResult<GameRecordResponse>> GetMyGameRecord(int gameId)
        {
            var result = await _gameService.GetMyGameRecord(gameId, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("all-user-record-in-game/{gameId}")]
        public async Task<ActionResult<PagedResponse<GameRecordResponse>>> GetAllGameRecord(int gameId,[FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetAllGameRecord(gameId, option, Account);
            return Ok(result);
        }

    }
}
