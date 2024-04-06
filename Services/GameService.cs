using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface IGameService
    {
        Task<AnswerHistoryResponse> AddAnswerHistory(AnswerHistoryRequest answerHistoryRequest, Account account);
        Task<GameResponse> CreateGame(GameCreate gameCreate, Account account);
        Task<GameResponse> EndById(int gameId, Account account);
        Task<PagedResponse<GameResponse>> GetAllByClassId(int classroomId, Models.Request.PagedRequest option, Account account);
        Task<PagedResponse<GameResponse>> GetAllByQuizBankId(int quizbankId, PagedRequest option, Account account);
        Task<PagedResponse<GameRecordResponse>> GetAllGameRecord(int gameId, PagedRequest option, Account account);
        Task<GameResponse> GetById(int gameId, Account account);
        Task<PagedResponse<GameQuizResponse>> GetQuizes(int gameId, PagedRequest option, Account account);
        Task<GameRecordResponse> GetUserGameRecord(int gameId, Account account, int userId);
        Task<PagedResponse<GameResponse>> GetMyJoined(PagedRequest option, Account account);
        Task Join(int gameId, Account account);
        Task<GameResponse> StartById(int gameId, Account account);
        Task<PagedResponse<AnswerHistoryResponse>> GetUserAnswerHistory(int gameId, int userId, PagedRequest option, Account account);
        Task<GameRecordResponse> SubmitTest(int gameId, AnswerHistoryRequest[] answerHistoryRequests, Account account);
        Task<GameQuizResponse> GetQuizByCurrentQuizId(int gameId, Account account, int? currentQuizId = null);
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

        public async Task<AnswerHistoryResponse> AddAnswerHistory(AnswerHistoryRequest answerHistoryRequest, Account account)
        {
            var game = await _context.Games.Include(g => g.QuizBank)
                                           .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                           .FirstOrDefaultAsync(g => g.Id == answerHistoryRequest.GameId);
            if (game == null)
            {
                throw new KeyNotFoundException("Could not find game");
            }

            var gameRecord = _context.GameRecords.FirstOrDefault(gr => gr.GameId == answerHistoryRequest.GameId && gr.AccountId == account.Id);
            if (gameRecord == null)
            {
                throw new AppException("User has not joined in the game yet");
            }

            if (game.Duration != null && DateTime.UtcNow > gameRecord.Created.AddMinutes(game.Duration ?? 0))
            {
                throw new AppException("Exceeding time limit");
            }

            var gameQuiz = await _context.GameQuizs.FirstOrDefaultAsync(q => q.Id == answerHistoryRequest.QuizId);
            if (gameQuiz == null)
            {
                throw new AppException("Can not find quiz");
            }

            var answerHistory = await _context.AnswerHistories.FirstOrDefaultAsync(a => a.GameRecordId == gameRecord.Id && a.GameQuizId == answerHistoryRequest.QuizId);

            answerHistory ??= new AnswerHistory()
            {
                GameRecordId = gameRecord.Id,
                GameQuizId = answerHistoryRequest.QuizId
            };
            answerHistory.UserAnswer = answerHistoryRequest.UserAnswer;
            var isCorrect = true;
            for (var i = 0; i < answerHistoryRequest.UserAnswer.Length; i++)
            {
                isCorrect = isCorrect && gameQuiz.CorrectAnswers[i].Equals(answerHistoryRequest.UserAnswer[i]);
            }
            _context.AnswerHistories.Update(answerHistory);
            await _context.SaveChangesAsync();

            return _mapper.Map<AnswerHistoryResponse>(answerHistory);
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
            game.NumberOfQuizzes = gameCreate.Amount;

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            game.GameQuizs = GetRandomQuizFormQuizbank(quizBank, gameCreate.Amount, game.Id, gameCreate.QuizTypes);
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
            if (classroom.Account.Id != account.Id && !classroom.AccountIds.Contains(account.Id))
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
            var gameRecords = await _context.GameRecords.Include(g => g.Account)
                                                        .Include(g => g.AnswerHistories)
                                                        .Where(gr => gr.GameId == gameId)
                                                        .ToPagedAsync(option, gr => true);

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

            if (AutoUpdateGameStatus(ref game))
            {
                _context.Games.Update(game);
                await _context.SaveChangesAsync();
            }

            return _mapper.Map<GameResponse>(game);
        }

        public async Task<GameRecordResponse> GetUserGameRecord(int gameId, Account account, int userId)
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

            var gameRecord = await _context.GameRecords.Include(g => g.AnswerHistories)
                .Include(g => g.Account)
                .FirstOrDefaultAsync(g => g.GameId == gameId && g.Account.Id == userId);

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

            if (AutoUpdateGameStatus(ref game))
            {
                _context.Games.Update(game);
                await _context.SaveChangesAsync();
            }

            if (game.Status != GameStatus.OnGoing)
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
            var gamequizes = await _context.GameQuizs.Where(gq => gq.GameId == gameId)
                                                     .ToPagedAsync(option, a => true);
            return new PagedResponse<GameQuizResponse>
            {
                Data = _mapper.Map<List<GameQuizResponse>>(gamequizes.Data),
                Metadata = gamequizes.Metadata,
            };
        }
        public async Task<PagedResponse<AnswerHistoryResponse>> GetUserAnswerHistory(int gameId, int userId, PagedRequest option, Account account)
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
            var gameRecord = _context.GameRecords.FirstOrDefault(gr => gr.GameId == gameId && gr.AccountId == account.Id);
            if (gameRecord == null)
            {
                throw new AppException("User has not joined in the game yet");
            }

            var answerHistory = await _context.AnswerHistories.Include(a => a.GameQuiz)
                .Where(a => a.GameRecordId == gameRecord.Id)
                .ToPagedAsync(option, a => true);
            return new PagedResponse<AnswerHistoryResponse>
            {
                Data = _mapper.Map<List<AnswerHistoryResponse>>(answerHistory.Data),
                Metadata = answerHistory.Metadata,
            };
        }

        public async Task<GameRecordResponse> SubmitTest(int gameId, AnswerHistoryRequest[] answerHistoryRequests, Account account)
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

            foreach (var answerHistoryRequest in answerHistoryRequests)
            {
                var gameQuiz = await _context.GameQuizs.FirstOrDefaultAsync(q => q.Id == answerHistoryRequest.QuizId);
                if (gameQuiz == null)
                {
                    throw new AppException("Can not find quiz");
                }
                var answerHistory = await _context.AnswerHistories.FirstOrDefaultAsync(a => a.GameRecordId == gameRecord.Id && a.GameQuizId == answerHistoryRequest.QuizId);

                answerHistory ??= new AnswerHistory()
                {
                    GameRecordId = gameRecord.Id,
                    GameQuizId = answerHistoryRequest.QuizId
                };
                answerHistory.UserAnswer = answerHistoryRequest.UserAnswer;
                var isCorrect = true;
                for (var i = 0; i < answerHistoryRequest.UserAnswer.Length; i++)
                {
                    isCorrect = isCorrect && gameQuiz.CorrectAnswers[i].Equals(answerHistoryRequest.UserAnswer[i]);
                }
                _context.AnswerHistories.Update(answerHistory);
            }

            await _context.SaveChangesAsync();

            return _mapper.Map<GameRecordResponse>(gameRecord);
        }

        private bool AutoUpdateGameStatus(ref Game game)
        {
            var now = DateTime.UtcNow;
            if (now < game.StartTime && game.Status != GameStatus.Created)
            {
                game.Status = GameStatus.Created;
                return true;
            }
            if (game.StartTime < now && now < game.EndTime && game.Status != GameStatus.OnGoing)
            {
                game.Status = GameStatus.OnGoing;
                return true;
            }
            if (game.EndTime < now && game.Status != GameStatus.Ended)
            {
                game.Status = GameStatus.Ended;
                return true;
            }
            return false;
        }
        private List<GameQuiz> GetRandomQuizFormQuizbank(QuizBank quizBank, int amount, int gameId, GameQuizType[] quizTypes)
        {
            var random = new Random();
            var maxtype = quizTypes.Length;
            var quizes = quizBank.Quizes.OrderBy(q => random.Next()).Take(amount).ToList();

            var result = new List<GameQuiz>();

            foreach (var quiz in quizes)
            {
                var type = quizTypes[random.Next(0, maxtype - 1)];
                var moreQuizes = quizes.Where(q => q != quiz).OrderBy(q => random.Next()).Take(3).ToList();
                var term = quiz.Question.Split("\n")[0];

                switch (type)
                {
                    case GameQuizType.MultipleChoice:
                        moreQuizes.Add(quiz);
                        result.Add(new GameQuiz
                        {
                            Questions = new List<string>() { term },
                            Answers = moreQuizes.OrderBy(q => random.Next()).Select(q => q.Answer).ToList(),
                            CorrectAnswers = new List<string> { quiz.Answer },
                            GameId = gameId,
                            Type = type,
                        });
                        break;
                    case GameQuizType.TrueFalse:
                        var correctAnswer = random.Next(0, 1) == 1;
                        var selectedAnswer = correctAnswer ? quiz.Answer : moreQuizes[0].Answer;

                        result.Add(new GameQuiz
                        {
                            Questions = new List<string>() { $"{term}\n{selectedAnswer}" },
                            Answers = new List<string>() { "True", "False" },
                            CorrectAnswers = new List<string>() { correctAnswer ? "True" : "False" },
                            GameId = gameId,
                            Type = type,
                        });
                        break;
                    case GameQuizType.ConstructedResponse:
                        result.Add(new GameQuiz
                        {
                            Questions = new List<string>() { term },
                            Answers = new List<string>(),
                            CorrectAnswers = new List<string>() { quiz.Answer },
                            GameId = gameId,
                            Type = type,
                        });
                        break;
                    case GameQuizType.Dnd:
                        moreQuizes.Add(quiz);
                        result.Add(new GameQuiz
                        {
                            Questions = moreQuizes.Select(q => q.Question).ToList(),
                            Answers = moreQuizes.Select(q => q.Answer).OrderBy(q => random.Next()).ToList(),
                            CorrectAnswers = moreQuizes.Select(q => q.Answer).ToList(),
                            GameId = gameId,
                            Type = type,
                        });
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        public async Task<GameQuizResponse?> GetQuizByCurrentQuizId(int gameId, Account account, int? currentQuizId = null)
        {
            var game = await _context.Games.Include(g => g.QuizBank).ThenInclude(q => q.Author)
                                          .Include(g => g.Classroom).ThenInclude(c => c.Account)
                                          .Include(g => g.GameQuizs)
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

            if (currentQuizId == null)
            {
                return _mapper.Map<GameQuizResponse>(game.GameQuizs?.FirstOrDefault());
            }

            var currentIndex = game.GameQuizs.FindIndex(q => q.Id == currentQuizId);
            if (currentIndex == -1)
            {
                throw new AppException($"Could not find quiz game with id {currentQuizId}");
            }
            if (currentIndex + 1 >= game.GameQuizs.Count)
            {
                return null;
            }

            return _mapper.Map<GameQuizResponse>(game.GameQuizs?[currentIndex + 1]);
        }
    }
}
