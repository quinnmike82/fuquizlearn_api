using fuquizlearn_api.Authorization;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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

        [SwaggerOperation("Create a Game/Test")]
        [Authorize]
        [HttpPost()]
        public async Task<ActionResult<GameResponse>> Create([FromBody] GameCreate gameCreate)
        {
            var result = await _gameService.CreateGame(gameCreate, Account);
            return Ok(result);
        }

        [SwaggerOperation("Join in a Game/Test")]
        [Authorize]
        [HttpPost("join-game/{gameId}")]
        public async Task<IActionResult> Join(int gameId)
        {
            await _gameService.Join(gameId, Account);
            return Ok();
        }

        [SwaggerOperation("Get all Game/Test in a classroom")]
        [Authorize]
        [HttpGet("get-all-by-classroom/{classroomId}")]
        public async Task<ActionResult<PagedResponse<GameResponse>>> GetAllByClassId(int classroomId, [FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetAllByClassId(classroomId, option, Account);
            return Ok(result);
        }

        [SwaggerOperation("Get all Game/Test created from in a quizbank")]
        [Authorize]
        [HttpGet("get-all-by-quizbank/{quizbankId}")]
        public async Task<ActionResult<PagedResponse<GameResponse>>> GetAllByQuizBankId(int quizbankId, [FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetAllByQuizBankId(quizbankId, option, Account);
            return Ok(result);
        }

        [SwaggerOperation("Get all Game/Test that current user joined")]
        [Authorize]
        [HttpGet("get-my-joined/")]
        public async Task<ActionResult<PagedResponse<GameResponse>>> GetMyJoined([FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetMyJoined(option, Account);
            return Ok(result);
        }

        [SwaggerOperation("Get a Game/Test by it's Id")]
        [Authorize]
        [HttpGet("{gameId}")]
        public async Task<ActionResult<GameResponse>> GetById(int gameId)
        {
            var result = await _gameService.GetById(gameId, Account);
            return Ok(result);
        }

        [SwaggerOperation("Get all quizzes in Game/Test")]
        [Authorize]
        [HttpGet("get-quizes-in-game/{gameId}")]
        public async Task<ActionResult<PagedResponse<GameQuizResponse>>> GetQuizes(int gameId,[FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetQuizes(gameId, option, Account);
            return Ok(result);
        }

        [SwaggerOperation("Start a Game/Test manually")]
        [Authorize]
        [HttpPut("start/{gameId}")]
        public async Task<ActionResult<GameResponse>> StartById(int gameId)
        {
            var result = await _gameService.StartById(gameId, Account);
            return Ok(result);
        }

        [SwaggerOperation("End a Game/Test manually")]
        [Authorize]
        [HttpPut("end/{gameId}")]
        public async Task<ActionResult<GameResponse>> EndById(int gameId)
        {
            var result = await _gameService.EndById(gameId, Account);
            return Ok(result);
        }

        [SwaggerOperation("Submit 1 quiz answer of current user in the game")]
        [Authorize]
        [HttpPost("add-answer-history/{gameId}")]
        public async Task<ActionResult<GameRecordResponse>> AddAnswerHistory(int gameId,[FromBody] AnswerHistoryRequest answerHistoryRequest)
        {
            var result = await _gameService.AddAnswerHistory(gameId, answerHistoryRequest, Account);
            return Ok(result);
        }

        [SwaggerOperation("Submit all quizzes answer of current user in the game")]
        [Authorize]
        [HttpPost("submit-test/{gameId}")]
        public async Task<ActionResult<GameRecordResponse>> SubmitTest(int gameId,[FromBody] AnswerHistoryRequest[] answerHistoryRequests)
        {
            var result = await _gameService.SubmitTest(gameId, answerHistoryRequests, Account);
            return Ok(result);
        }

        [SwaggerOperation("Get a record of a user in a game/test")]
        [Authorize]
        [HttpGet("{gameId}/user-record/{userId}")]
        public async Task<ActionResult<GameRecordResponse>> GetMyGameRecord(int gameId, int userId)
        {
            var result = await _gameService.GetUserGameRecord(gameId, Account, userId);
            return Ok(result);
        }

        [SwaggerOperation("Get records of all users in a game/test")]
        [Authorize]
        [HttpGet("{gameId}/all-user-record")]
        public async Task<ActionResult<PagedResponse<GameRecordResponse>>> GetAllGameRecord(int gameId,[FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetAllGameRecord(gameId, option, Account);
            return Ok(result);
        }

        [SwaggerOperation("Get answer history of a user in a game/test")]
        [Authorize]
        [HttpGet("{gameId}/user-answer-history/{userId}")]
        public async Task<ActionResult<PagedResponse<AnswerHistoryResponse>>> GetUserAnswerHistory(int gameId, int userId, [FromQuery] PagedRequest option)
        {
            var result = await _gameService.GetUserAnswerHistory(gameId, userId, option, Account);
            return Ok(result);
        }

    }
}
