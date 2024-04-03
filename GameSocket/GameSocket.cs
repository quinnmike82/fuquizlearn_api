using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.SignalR;

namespace fuquizlearn_api.GameSocket
{
    public class GameSocket : Hub
    {
        public Account Account => (Account)Context.Items["Account"];
        private readonly IGameService _gameService;

        public GameSocket(IGameService gameService)
        {
            _gameService = gameService;
        }

        public async Task JoinGame(int gameId)
        {
                var joinedUsers = await _gameService.Join(gameId, Account);
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
                await Clients.Group(gameId.ToString()).SendAsync("Joined Success", joinedUsers);
        }

        public async Task AddAnswerHistory(AnswerHistoryRequest answerHistoryRequest)
        {
                var result = await _gameService.AddAnswerHistory(answerHistoryRequest, Account);
                await Clients.Client(Context.ConnectionId).SendAsync("Answer Result", result);
        }

        public async Task GetQuizes(int gameId, PagedRequest option)
        {
                var result = await _gameService.GetQuizes(gameId, option, Account);
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
