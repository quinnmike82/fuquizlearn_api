using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
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
        Task<PagedResponse<GameResponse>> GetAllByQuizBankId(int quizbankId, PagedRequest option, Account account);
        Task<PagedResponse<GameRecordResponse>> GetAllGameRecord(int gameId, PagedRequest option, Account account);
        Task<GameResponse> GetById(int gameId, Account account);
        Task<PagedResponse<GameQuizResponse>> GetQuizes(int gameId, PagedRequest option, Account account);
        Task<GameRecordResponse> GetMyGameRecord(int gameId, Account account);
        Task<PagedResponse<GameResponse>> GetMyJoined(PagedRequest option, Account account);
        Task Join(int gameId, Account account);
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
            var game = await _context.Games.Include(g => g.QuizBank)
                                           .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                           .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }

            var gameRecord = _context.GameRecords.FirstOrDefault(gr => gr.GameId == gameId && gr.AccountId == account.Id);
            if (gameRecord == null)
            {
                throw new AppException("User has not joined in the game yet");
            }

            if (game.Duration != null && DateTime.UtcNow > gameRecord.Created.AddMinutes(game.Duration ?? 0))
            {
                throw new AppException("Exceeding time limit");
            }


            var quiz = await _context.Quizes.FirstOrDefaultAsync(q => q.Id == answerHistoryRequest.QuizId);
            if (quiz == null)
            {
                throw new AppException("Could not find quiz");
            }
            var answerHistory = _mapper.Map<AnswerHistory>(answerHistoryRequest);
            answerHistory.Quiz = _mapper.Map<QuizResponse>(quiz);
            answerHistory.IsCorrect = quiz.Answer.Equals(answerHistory.UserAnswer, StringComparison.OrdinalIgnoreCase);
            gameRecord.AnswerHistories.RemoveAll(ah => ah.QuizId == answerHistory.QuizId);
            gameRecord.AnswerHistories.Add(answerHistory);
            _context.GameRecords.Update(gameRecord);
            await _context.SaveChangesAsync();

            return _mapper.Map<GameRecordResponse>(gameRecord);
        }

        public async Task<GameResponse> CreateGame(GameCreate gameCreate, Account account)
        {

            var quizBank = await _context.QuizBanks.Include(qb => qb.Quizes).FirstOrDefaultAsync(qb => qb.Id == gameCreate.QuizBankId);
            if (quizBank == null) throw new KeyNotFoundException($"Can not found quizbank with id: {gameCreate.QuizBankId}");

            if (gameCreate.ClassroomId != null)
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

                if (classroom.BankIds == null || !classroom.BankIds.Contains(gameCreate.QuizBankId))
                {
                    throw new AppException("QuizBank is not included in classroom");
                }
            }
            else if (quizBank.Visibility != Visibility.Public && quizBank.Author.Id != account.Id)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            if (gameCreate.Amount > quizBank.Quizes.Count)
            {
                throw new AppException("Amount is larger than quizbank size");
            }


            var game = _mapper.Map<Game>(gameCreate);
            game.ClassroomId = gameCreate.ClassroomId;
            game.QuizBankId = quizBank.Id;
            game.Status = GameStatus.Created;

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            game.GameQuizs = GetRandomQuizFormQuizbank(quizBank, gameCreate.Amount, game.Id);
            _context.Games.Update(game);
            await _context.SaveChangesAsync();
            return _mapper.Map<GameResponse>(game);
        }

        public async Task<GameResponse> EndById(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.QuizBank).ThenInclude(qb => qb.Author)
                                            .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                            .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }

            var permission = game.QuizBank.Visibility == Visibility.Public || game.QuizBank.Author.Id == account.Id;
            if (game.Classroom != null)
            {
                permission = account.Id == game.Classroom.Account.Id
                    || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            }
            if (!permission)
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

        public async Task<PagedResponse<GameRecordResponse>> GetAllGameRecord(int gameId, PagedRequest option, Account account)
        {
            var game = await _context.Games.Include(g => g.QuizBank).ThenInclude(qb => qb.Author)
                                            .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                            .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }

            var permission = game.QuizBank.Visibility == Visibility.Public || game.QuizBank.Author.Id == account.Id;
            if (game.Classroom != null)
            {
                permission = account.Id == game.Classroom.Account.Id
                    || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            }
            if (!permission)
            {
                throw new UnauthorizedAccessException("Unauthorize");
            }
            var gameRecords = await _context.GameRecords.Where(gr => gr.GameId == gameId).ToPagedAsync(option, gr => true);

            return new PagedResponse<GameRecordResponse>
            {
                Data = _mapper.Map<List<GameRecordResponse>>(gameRecords.Data),
                Metadata = gameRecords.Metadata
            };
        }

        public async Task<GameResponse> GetById(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.QuizBank).ThenInclude(q => q.Author)
                                           .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                           .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }
            var permission = game.QuizBank.Visibility == Visibility.Public || game.QuizBank.Author.Id == account.Id;

            if (game.Classroom != null)
            {
                permission = game.Classroom.Account.Id == account.Id
                        || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            }

            if (!permission)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            AutoUpdateGameStatus(ref game);
            return _mapper.Map<GameResponse>(game);
        }

        public async Task<GameRecordResponse> GetMyGameRecord(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.QuizBank).ThenInclude(q => q.Author)
                               .Include(g => g.Classroom).ThenInclude(c => c.Account)
                               .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }
            var permission = game.QuizBank.Visibility == Visibility.Public || game.QuizBank.Author.Id == account.Id;

            if (game.Classroom != null)
            {
                permission = game.Classroom.Account.Id == account.Id
                        || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            }

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
            var game = await _context.Games.Include(g => g.QuizBank).ThenInclude(qb => qb.Author)
                                 .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                 .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }

            var permission = game.QuizBank.Visibility == Visibility.Public || game.QuizBank.Author.Id == account.Id;
            if (game.Classroom != null)
            {
                permission = account.Id == game.Classroom.Account.Id
                    || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            }
            if (!permission)
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


        public async Task<PagedResponse<GameResponse>> GetAllByQuizBankId(int quizbankId, PagedRequest option, Account account)
        {
            var quizBank = await _context.QuizBanks.Include(qb => qb.Author).FirstOrDefaultAsync(qb => qb.Id == quizbankId);
            if (quizBank == null)
            {
                throw new KeyNotFoundException($"Can not found quizbank with id: {quizBank}");
            }
            if (quizBank.Visibility != Visibility.Public && quizBank.Author.Id != account.Id)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            var games = await _context.Games.Where(g => g.QuizBankId == quizbankId)
            .ToPagedAsync(option,
                g => g.GameName.IndexOf(HttpUtility.UrlDecode(option.Search, Encoding.ASCII), StringComparison.OrdinalIgnoreCase) >= 0);
            return new PagedResponse<GameResponse>
            {
                Data = _mapper.Map<List<GameResponse>>(games.Data),
                Metadata = games.Metadata
            };
        }

        public async Task<PagedResponse<GameResponse>> GetMyJoined(PagedRequest option, Account account)
        {
            var gameRecord = await _context.GameRecords.Include(gr => gr.Game)
                                                        .Where(gr => gr.AccountId == account.Id)
                                                        .Select(gr => gr.Game)
                                                        .ToPagedAsync(option, a => true);
            return new PagedResponse<GameResponse>
            {
                Data = _mapper.Map<List<GameResponse>>(gameRecord.Data),
                Metadata = gameRecord.Metadata
            };
        }

        public async Task Join(int gameId, Account account)
        {
            var game = await _context.Games.Include(g => g.QuizBank).ThenInclude(q => q.Author)
                                           .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                           .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }
            var permission = game.QuizBank.Visibility == Visibility.Public || game.QuizBank.Author.Id == account.Id;

            if (game.Classroom != null)
            {
                permission = game.Classroom.Account.Id == account.Id
                        || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            }

            if (!permission)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            AutoUpdateGameStatus(ref game);
            if (game.Status != GameStatus.OnGoing )
            {
                throw new AppException("Game is not started or has been ended");
            }

            var gameRecord = await _context.GameRecords.FirstOrDefaultAsync(g => g.GameId == gameId && g.Account.Id == account.Id);

            if (gameRecord != null)
            {
                throw new AppException("User already joined in the game");
            }
            gameRecord = new GameRecord
            {
                AccountId = account.Id,
                GameId = gameId,
                AnswerHistories = new List<AnswerHistory>()
            };

            _context.GameRecords.Add(gameRecord);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResponse<GameQuizResponse>> GetQuizes(int gameId, PagedRequest option, Account account)
        {
            var game = await _context.Games.Include(g => g.QuizBank).ThenInclude(q => q.Author)
                                           .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                           .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }
            var permission = game.QuizBank.Visibility == Visibility.Public || game.QuizBank.Author.Id == account.Id;

            if (game.Classroom != null)
            {
                permission = game.Classroom.Account.Id == account.Id
                        || (game.Classroom.AccountIds != null && game.Classroom.AccountIds.Contains(account.Id));
            }

            if (!permission)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }
            var gamequizes = await _context.GameQuizs.Include(gq => gq.Quiz)
                                                     .Where(gq => gq.GameId == gameId)
                                                     .ToPagedAsync(option, a => true);
            return new PagedResponse<GameQuizResponse>
            {
                Data = _mapper.Map<List<GameQuizResponse>>(gamequizes.Data),
                Metadata = gamequizes.Metadata,
            };
        }

        private void AutoUpdateGameStatus(ref Game game)
        {
            var now = DateTime.UtcNow;
            if (game.StartTime < now && now < game.EndTime) game.Status = GameStatus.OnGoing;
            if (game.EndTime < now) game.Status = GameStatus.Ended;
        }
        private List<GameQuiz> GetRandomQuizFormQuizbank(QuizBank quizBank, int amount, int gameId)
        {
            var random = new Random();
            var maxtype = System.Enum.GetValues(typeof(GameQuizType)).Length;
            var quizes = quizBank.Quizes.OrderBy(q => random.Next()).Take(amount).ToList();

            return quizes.Select(q => new GameQuiz
            {
                Quiz = q,
                Type = (GameQuizType)random.Next(0, maxtype - 1),
                GameId = gameId
            }).ToList();
        }
    }
}
