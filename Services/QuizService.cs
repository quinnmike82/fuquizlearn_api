
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using AutoMapper;
using fuquizlearn_api.Enum;
using System.Security.Principal;
using fuquizlearn_api.Models.Quiz;

namespace fuquizlearn_api.Services
{
    public interface IQuizService
    {
        IEnumerable<QuizResponse> GetAllQuizFromBank(int bankId, Account currentUser);
        QuizResponse AddQuizInBank(Account currentUser, QuizCreate model, int bankId);
        QuizResponse UpdateQuizInBank(int bankId, int quizId, QuizUpdate model, Account currentUser);
        void DeleteQuizInBank(int bankId, int quizId, Account account);
    }

    public class QuizService : IQuizService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

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

        public void DeleteQuizInBank(int bankId, int quizId, Account account)
        {
            CheckQuizBank(bankId, account);
            var quiz = GetQuiz(bankId, quizId);
            _context.Quizes.Remove(quiz);
            _context.SaveChanges();
        }

        public IEnumerable<QuizResponse> GetAllQuizFromBank(int bankId, Account currentUser)
        {
            CheckQuizBank(bankId, currentUser);
            var quizes = _context.Quizes.Where(q => q.QuizBankId == bankId);
            return _mapper.Map<IEnumerable<QuizResponse>>(quizes); ;
        }

        public QuizResponse UpdateQuizInBank(int bankId, int quizId, QuizUpdate model, Account currentUser)
        {
            CheckQuizBank(bankId, currentUser);

            var quiz = GetQuiz(bankId, quizId);
            _mapper.Map(model, quiz);
            quiz.Choices = model.Choices ?? quiz.Choices;
            quiz.Updated = DateTime.UtcNow;
            _context.SaveChanges();

            return _mapper.Map<QuizResponse>(quiz); ;
        }

        private QuizBank CheckQuizBank(int bankId, Account currentUser)
        {
            var quizBank = _context.QuizBanks.Find(bankId);
            if (quizBank == null) throw new KeyNotFoundException("Could not find quizbank");
            if (quizBank.Visibility == Visibility.Public || quizBank.Author.Id == currentUser.Id || currentUser.Role == Role.Admin) return quizBank;
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
