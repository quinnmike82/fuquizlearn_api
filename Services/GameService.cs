using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface IGameService
    {
        Task<GameRecordResponse> AddAnswerHistory(int gameId, AnswerHistoryRequest answerHistoryRequest, Account account);
        Task<GameResponse> CreateGame(GameCreate gameCreate, Account account);
        Task<GameResponse> EndById(int gameId, Account account);
        Task<PagedResponse<GameResponse>> GetAllByClassId(int classroomId, Models.Request.PagedRequest option, Account account);
        Task<List<GameRecordResponse>> GetAllGameRecord(int gameId, Account account);
        Task<GameResponse> GetById(int gameId, Account account);
        Task<GameRecordResponse> GetMyGameRecord(int gameId, Account account);
        Task<GameResponse> StartById(int gameId, Account account);
    }
    public class GameService : IGameService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public GameService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<GameRecordResponse> AddAnswerHistory(int gameId, AnswerHistoryRequest answerHistoryRequest, Account account)
        {
            var game = await _context.Games.Include(g => g.Classroom)
                                            .ThenInclude(c => c.Account)
                                            .FirstOrDefaultAsync(g => g.Id == gameId);
            if(game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }

            var permission = game.Classroom.Account.Id == account.Id
                            || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            if(!permission)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            if(game.Status != GameStatus.OnGoing)
            {
                throw new AppException("Game is not started or has been ended");
            }

            var gameRecord = await _context.GameRecords.FirstOrDefaultAsync(g => g.GameId == gameId && g.Account.Id == account.Id);

            if (gameRecord == null)
            {
                gameRecord = new GameRecord
                {
                    AccountId = account.Id,
                    GameId = gameId,
                    AnswerHistories = new List<AnswerHistory>()
                };
            }
            var quiz = await _context.Quizes.FirstOrDefaultAsync(q => q.Id == answerHistoryRequest.QuizId);
            if(quiz == null)
            {
                throw new AppException("Could not find quiz");
            }
            var answerHistory = _mapper.Map<AnswerHistory>(answerHistoryRequest);
            answerHistory.Quiz = _mapper.Map<QuizResponse>(quiz);
            gameRecord.AnswerHistories.RemoveAll(ah => ah.QuizId == answerHistory.QuizId);
            gameRecord.AnswerHistories.Add(answerHistory);
            _context.GameRecords.Update(gameRecord);
            await _context.SaveChangesAsync();

            return _mapper.Map<GameRecordResponse>(gameRecord);
        }

        public async Task<GameResponse> CreateGame(GameCreate gameCreate, Account account)
        {
            var classroom = await _context.Classrooms.FindAsync(gameCreate.ClassroomId);
            if (classroom == null)
            {
                throw new KeyNotFoundException($"Can not found classroom with id: {gameCreate.ClassroomId}");
            }
            if (classroom.Account.Id != account.Id)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            var game = _mapper.Map<Game>(gameCreate);
            game.Classroom = classroom;
            game.Status = GameStatus.Created;
            gameCreate.QuizIds = gameCreate.QuizIds.Distinct().ToList();
            var quizes = await _context.Quizes.Where(q => gameCreate.QuizIds.Contains(q.Id)).ToListAsync();
            if (gameCreate.QuizIds.Count > quizes.Count)
            {
                throw new KeyNotFoundException($"Can not found quizes with id: {String.Join(",", gameCreate.QuizIds.Except(quizes.Select(q => q.Id)).ToArray())}");
            }
            game.GameQuizs = new List<GameQuiz>();

            foreach (var item in quizes)
            {
                var gameQuiz = new GameQuiz
                {
                    Quiz = _mapper.Map<QuizResponse>(item),
                    Type = GetRandomQuizType()
                };
                game.GameQuizs.Add(gameQuiz);
            };

            _context.Games.Add(game);
            try
            {

            await _context.SaveChangesAsync();
            }catch (Exception ex)
            {

            }

            return _mapper.Map<GameResponse>(game);
        }

        public async Task<GameResponse> EndById(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.Classroom)
                                            .ThenInclude(c => c.Account)
                                            .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }
            if (account.Id != game.Classroom.Account.Id)
            {
                throw new UnauthorizedAccessException("Unauthorize");
            }
            if (game.Status != GameStatus.OnGoing)
            {
                throw new AppException("Game is not started or has been ended");
            }

            game.Status = GameStatus.OnGoing;
            await _context.SaveChangesAsync();

            return _mapper.Map<GameResponse>(game);
        }

        public async Task<PagedResponse<GameResponse>> GetAllByClassId(int classroomId, PagedRequest option, Account account)
        {
            var classroom = await _context.Classrooms.FindAsync(classroomId);
            if (classroom == null)
            {
                throw new KeyNotFoundException($"Can not found classroom with id: {classroomId}");
            }
            if (classroom.Account.Id != account.Id || classroom.AccountIds.Contains(account.Id))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            var games = await _context.Games.Where(g => g.ClassroomId == classroomId)
            .ToPagedAsync(option,
                g => g.GameName.IndexOf(HttpUtility.UrlDecode(option.Search, Encoding.ASCII), StringComparison.OrdinalIgnoreCase) >= 0);
            return new PagedResponse<GameResponse>
            {
                Data = _mapper.Map<List<GameResponse>>(games.Data),
                Metadata = games.Metadata
            };
        }

        public async Task<List<GameRecordResponse>> GetAllGameRecord(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.Classroom)
                                           .ThenInclude(c => c.Account)
                                           .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }

            var permission = game.Classroom.Account.Id == account.Id
                            || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            if (!permission)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            var gameRecords = await _context.GameRecords.Where(gr => gr.GameId == gameId).ToListAsync();

            if (gameRecords == null || gameRecords.Count == 0)
            {
                throw new KeyNotFoundException("There is no record");
            }

            return _mapper.Map<List<GameRecordResponse>>(gameRecords);
        }

        public async Task<GameResponse> GetById(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.Classroom).FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null) throw new KeyNotFoundException("Could not find game");
            var isAllow = account.Id == game.Classroom.Account.Id
                || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            if (!isAllow)
            {
                throw new UnauthorizedAccessException("Unauthorize");
            }

            return _mapper.Map<GameResponse>(game);
        }

        public async Task<GameRecordResponse> GetMyGameRecord(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.Classroom)
                                            .ThenInclude(c => c.Account)
                                            .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }

            var permission = game.Classroom.Account.Id == account.Id
                            || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            if (!permission)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            var gameRecord = await _context.GameRecords.FirstOrDefaultAsync(g => g.GameId == gameId && g.Account.Id == account.Id);

            if (gameRecord == null)
            {
               throw new KeyNotFoundException("There is no record");
            }

            return _mapper.Map<GameRecordResponse>(gameRecord);
        }

        public async Task<GameResponse> StartById(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.Classroom)
                                            .ThenInclude(c => c.Account)
                                            .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }
            if (account.Id != game.Classroom.Account.Id)
            {
                throw new UnauthorizedAccessException("Unauthorize");
            }
            if (game.Status != GameStatus.Created)
            {
                throw new AppException("Game already started or ended");
            }

            game.Status = GameStatus.OnGoing;
            await _context.SaveChangesAsync();

            return _mapper.Map<GameResponse>(game);
        }

        private GameQuizType GetRandomQuizType()
        {
            var random = new Random();
            var max = System.Enum.GetValues(typeof(GameQuizType)).Length;
            return (GameQuizType)random.Next(0, max - 1);
        }
    }
}
