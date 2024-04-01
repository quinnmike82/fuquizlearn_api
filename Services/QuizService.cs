using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface IQuizService
    {
        Task<PagedResponse<QuizResponse>> GetAllQuizFromBank(int bankId, Account currentUser, QuizPagedRequest options);
        QuizResponse AddQuizInBank(Account currentUser, QuizCreate model, int bankId);
        QuizResponse UpdateQuizInBank(int bankId, int quizId, QuizUpdate model, Account currentUser);
        QuizResponse GetQuizById(int quizId);
        QuizResponse UpdateQuiz(int quizId, QuizUpdate model); 
        void DeleteQuizInBank(int bankId, int quizId, Account account);
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

        public QuizResponse AddQuizInBank(Account currentUser, QuizCreate model, int bankId)
        {
            CheckQuizBank(bankId, currentUser);

            var quiz = _mapper.Map<Quiz>(model);
            quiz.QuizBankId = bankId;
            quiz.Created = DateTime.UtcNow;

            _context.Quizes.Add(quiz);
            _context.SaveChanges();
            return _mapper.Map<QuizResponse>(quiz);
        }

        public QuizResponse GetQuizById(int quizId)
        {
            var quiz = _context.Quizes.Find(quizId);    
            if (quiz == null) throw new KeyNotFoundException("Could not find the quiz");
            return _mapper.Map<QuizResponse>(quiz);     
        }

        public QuizResponse UpdateQuiz(int quizId, QuizUpdate model)
        {
            var quiz = _context.Quizes.Find(quizId);
            if (quiz == null) throw new KeyNotFoundException("Could not find the quiz");
            _mapper.Map(model, quiz);
            quiz.Updated = DateTime.UtcNow;
            _context.SaveChanges();
            return _mapper.Map<QuizResponse>(quiz); 
        }

        public void DeleteQuizInBank(int bankId, int quizId, Account account)
        {
            CheckQuizBank(bankId, account);
            var quiz = GetQuiz(bankId, quizId);
            _context.Quizes.Remove(quiz);
            _context.SaveChanges();
        }

        public async Task<PagedResponse<QuizResponse>> GetAllQuizFromBank(int bankId, Account currentUser,
            QuizPagedRequest options)
        {
            CheckQuizBank(bankId, currentUser);
            if (options.IsGetAll)
            {
                var quizes = _context.Quizes.Where(q => q.QuizBankId == bankId).ToList();
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

        public QuizResponse UpdateQuizInBank(int bankId, int quizId, QuizUpdate model, Account currentUser)
        {
            CheckQuizBank(bankId, currentUser);

            var quiz = GetQuiz(bankId, quizId);
            _mapper.Map(model, quiz);
            quiz.Updated = DateTime.UtcNow;
            _context.SaveChanges();

            return _mapper.Map<QuizResponse>(quiz);
            ;
        }

        private QuizBank CheckQuizBank(int bankId, Account currentUser)
        {
            var quizBank = _context.QuizBanks.Find(bankId);
            if (quizBank == null) throw new KeyNotFoundException("Could not find quizbank");
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

        private Quiz GetQuiz(int bankId, int quizId)
        {
            var quiz = _context.Quizes.FirstOrDefault(x => x.Id == quizId && x.QuizBankId == bankId);
            if (quiz == null)
            {
                throw new KeyNotFoundException("Could not find the quiz");
            }

            return quiz;
        }
    }
}