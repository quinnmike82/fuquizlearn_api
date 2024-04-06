using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.SignalR;

namespace fuquizlearn_api.GameSocket
{
    [Authorize]
    public class GameSocket : Hub
    {
        private Account Account;
        private readonly IGameService _gameService;
        private readonly IAccountService _accountService;

        public GameSocket(IGameService gameService, IAccountService accountService)
        {
            _gameService = gameService;
            _accountService = accountService;
        }

        public override Task OnConnectedAsync()
        {
            var email = Context.User?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (email == null)
            {
                throw new HubException("Email not found");
            }
            this.Account = _accountService.GetByEmail(email);
            return base.OnConnectedAsync();
        }

        public async Task JoinGame(int gameId)
        {
            await _gameService.Join(gameId, Account);
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
            var firstQuiz = await _gameService.GetQuizByCurrentQuizId(gameId, Account);
            await Clients.Client(Context.ConnectionId).SendAsync("Joined Success", firstQuiz);
        }

        public async Task AddAnswerHistory(AnswerHistoryRequest answerHistoryRequest)
        {
            var result = await _gameService.AddAnswerHistory(answerHistoryRequest, Account);
            var nextQuiz = await _gameService.GetQuizByCurrentQuizId(answerHistoryRequest.GameId, Account, answerHistoryRequest.QuizId);
            await Clients.Client(Context.ConnectionId).SendAsync("Answer Result", result, nextQuiz);
        }

        public async Task GetQuizes(int gameId, int currentQuizId)
        {
            var result = await _gameService.GetQuizByCurrentQuizId(gameId, Account, currentQuizId);
            await Clients.Client(Context.ConnectionId).SendAsync("Get Quizes", result);
        }

        public async Task LeaveGame(int gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());
            await Clients.Client(Context.ConnectionId).SendAsync("Leave Game", "Leaved game");
            Context.Abort();
        }

    }
}
