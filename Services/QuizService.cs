using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface IQuizService
    {
        Task<List<Quiz>> GetAll(int bankId); // this should not be used in user
        Task<PagedResponse<QuizResponse>> GetAllQuizFromBank(int bankId, Account currentUser, QuizPagedRequest options);
        Task<QuizResponse> AddQuizInBank(Account currentUser, QuizCreate model, int bankId);
        Task<QuizResponse> UpdateQuizInBank(int bankId, int quizId, QuizUpdate model, Account currentUser);
        Task<QuizResponse> GetQuizById(int quizId);
        Task<QuizResponse> UpdateQuiz(int quizId, QuizUpdate model);
        Task DeleteQuizInBank(int bankId, int quizId, Account account);
    }

    public class QuizService : IQuizService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private IQuizService _quizServiceImplementation;

        public QuizService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<Quiz>> GetAll(int bankId)
        {
                var quizes = await _context.Quizes.Where(q => q.QuizBankId == bankId).ToListAsync();
                return quizes ?? throw new KeyNotFoundException("Quiz.not_found");
        }

        public async Task<QuizResponse> AddQuizInBank(Account currentUser, QuizCreate model, int bankId)
        {
            CheckQuizBank(bankId, currentUser);

            var quiz = _mapper.Map<Quiz>(model);
            quiz.QuizBankId = bankId;
            quiz.Created = DateTime.UtcNow;

            _context.Quizes.Add(quiz);
            await _context.SaveChangesAsync();
            return _mapper.Map<QuizResponse>(quiz);
        }

        public async Task<QuizResponse> GetQuizById(int quizId)
        {
            var quiz = await _context.Quizes.FindAsync(quizId);    
            if (quiz == null) throw new KeyNotFoundException("Quiz.not_found");
            return _mapper.Map<QuizResponse>(quiz);     
        }

        public async Task<QuizResponse> UpdateQuiz(int quizId, QuizUpdate model)
        {
            var quiz = await _context.Quizes.FindAsync(quizId);
            if (quiz == null) throw new KeyNotFoundException("Quiz.not_found");
            _mapper.Map(model, quiz);
            quiz.Updated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return _mapper.Map<QuizResponse>(quiz); 
        }

        public async Task DeleteQuizInBank(int bankId, int quizId, Account account)
        {
            await CheckQuizBank(bankId, account);
            var quiz = await GetQuiz(bankId, quizId);
            _context.Quizes.Remove(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResponse<QuizResponse>> GetAllQuizFromBank(int bankId, Account currentUser,
            QuizPagedRequest options)
        {
            await CheckQuizBank(bankId, currentUser);
            if (options.IsGetAll)
            {
                var quizes = await _context.Quizes.Where(q => q.QuizBankId == bankId).ToListAsync();
                return new PagedResponse<QuizResponse>
                {
                    Data = _mapper.Map<IEnumerable<QuizResponse>>(quizes),
                    Metadata = new PagedMetadata(0, quizes.Count, quizes.Count, false)
                };
            }

            var pagedQuizes = await _context.Quizes.Where(q => q.QuizBankId == bankId).ToPagedAsync(options,
                q => q.Question.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
            return new PagedResponse<QuizResponse>
            {
                Data = _mapper.Map<IEnumerable<QuizResponse>>(pagedQuizes.Data),
                Metadata = pagedQuizes.Metadata
            };
        }

        public async Task<QuizResponse> UpdateQuizInBank(int bankId, int quizId, QuizUpdate model, Account currentUser)
        {
            await CheckQuizBank(bankId, currentUser);

            var quiz = await GetQuiz(bankId, quizId);
            _mapper.Map(model, quiz);
            quiz.Updated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return _mapper.Map<QuizResponse>(quiz);
        }

        private async Task<QuizBank> CheckQuizBank(int bankId, Account currentUser)
        {
            var quizBank = await _context.QuizBanks.Include(c => c.Author).FirstOrDefaultAsync(c => c.Id == bankId && c.DeletedAt == null);
            if (quizBank == null) throw new KeyNotFoundException("Quizbank.not_found");
            if (quizBank.Visibility == Visibility.Public)
            {
                return quizBank;
            }
            else
            {
                if (currentUser != null)
                {
                    if (quizBank.Author.Id == currentUser.Id || currentUser.Role == Role.Admin) return quizBank;
                }
            }

            throw new UnauthorizedAccessException();
        }

        private async Task<Quiz> GetQuiz(int bankId, int quizId)
        {
            var quiz = await _context.Quizes.FirstOrDefaultAsync(x => x.Id == quizId && x.QuizBankId == bankId);
            if (quiz == null)
            {
                throw new KeyNotFoundException("Quiz.not_found");
            }

            return quiz;
        }
    }
}