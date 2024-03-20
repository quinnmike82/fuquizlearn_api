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
        [HttpGet("get-all-by-classroomID/{classroomId}")]
        public async Task<ActionResult<PagedResponse<GameResponse>>> GetAllByClassId(int classroomId, [FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetAllByClassId(classroomId, option, Account);
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
        [HttpGet("my-game-record/{gameId}")]
        public async Task<ActionResult<GameRecordResponse>> GetMyGameRecord(int gameId)
        {
            var result = await _gameService.GetMyGameRecord(gameId, Account);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("all-game-record/{gameId}")]
        public async Task<ActionResult<List<GameRecordResponse>>> GetAllGameRecord(int gameId)
        {
            var result = await _gameService.GetAllGameRecord(gameId, Account);
            return Ok(result);
        }
    }
}
