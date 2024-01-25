
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using AutoMapper;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Enum;

namespace fuquizlearn_api.Services
{
    public interface IQuizBankService
    {
        IEnumerable<QuizBankResponse> GetAll();
        QuizBankResponse GetById(int id);
        QuizBankResponse Create(Account currentUser, QuizBankCreate model);
        QuizBankResponse Update(int id, QuizBankUpdate model, Account currentUser);
        void Delete(int id, Account currentUser);
        void DeleteQuiz(int id, int quizId, Account account);
        QuizBankResponse UpdateQuiz(int id, int quizId, QuizUpdate model, Account account);
        QuizBankResponse AddQuiz(Account account, QuizCreate model, int id);
    }

    public class QuizBankService : IQuizBankService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public QuizBankService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public QuizBankResponse AddQuiz(Account account, QuizCreate model, int id)
        {
            var quizBank = this.GetQuizBank(id);
            if (account.Id != quizBank.Author.Id) throw new UnauthorizedAccessException();

            var quiz = _mapper.Map<Quiz>(model);
            quiz.Created = DateTime.UtcNow;

            quizBank.Quizes.Add(quiz);
            quizBank.Updated = DateTime.UtcNow;
            _context.SaveChanges();

            return _mapper.Map<QuizBankResponse>(quizBank);
        }

        public QuizBankResponse Create(Account currentUser, QuizBankCreate model)
        {

            if(model.Visibility == null)
            {
                model.Visibility = Visibility.Public;
            }

            var quizBank = _mapper.Map<QuizBank>(model);
            quizBank.Created = DateTime.UtcNow;
            quizBank.Author = currentUser;
            _context.QuizBanks.Add(quizBank);
            _context.SaveChanges();

            return _mapper.Map<QuizBankResponse>(quizBank);
        }


        public void Delete(int id, Account currentUser)
        {
            var quizBank = GetQuizBank(id);
            if (currentUser.Role != Role.Admin && currentUser.Id != quizBank.Author.Id)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            _context.QuizBanks.Remove(quizBank);
            _context.SaveChanges();
        }

        public void DeleteQuiz(int id, int quizId, Account account)
        {
            var quizBank = GetQuizBank(id);
            if (account.Role != Role.Admin && account.Id != quizBank.Author.Id)
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            var quiz = quizBank.Quizes.FirstOrDefault(x => x.Id == quizId);
            if (quiz == null)
            {
                throw new KeyNotFoundException("Could not find the quiz");
            }
            quizBank.Quizes.Remove(quiz);
            quizBank.Updated = DateTime.UtcNow;

            _context.QuizBanks.Update(quizBank);
            _context.SaveChanges();
        }

        public IEnumerable<QuizBankResponse> GetAll()
        {
            var quizBanks = _context.QuizBanks;
            return _mapper.Map<IList<QuizBankResponse>>(quizBanks);
        }

        public QuizBankResponse GetById(int id)
        {
            var quizBank = GetQuizBank(id);
            return _mapper.Map<QuizBankResponse>(quizBank);
        }

        public QuizBankResponse Update(int id, QuizBankUpdate model, Account currentUser)
        {
            var quizBank = this.GetQuizBank(id);
            if (currentUser.Id != quizBank.Author.Id) throw new UnauthorizedAccessException();

            _mapper.Map(model, quizBank);
            quizBank.Updated = DateTime.UtcNow;

            _context.QuizBanks.Update(quizBank);
            _context.SaveChanges();

            return _mapper.Map<QuizBankResponse>(quizBank);
        }

        public QuizBankResponse UpdateQuiz(int id, int quizId, QuizUpdate model, Account account)
        {
            var quizBank = this.GetQuizBank(id);
            if (account.Id != quizBank.Author.Id) throw new UnauthorizedAccessException();

            var quiz = quizBank.Quizes.FirstOrDefault(x => x.Id == quizId);
            if (quiz == null)
            {
                throw new KeyNotFoundException("Could not find the quiz");
            }
                _mapper.Map(model, quiz);
                quiz.Choices = model.Choices ?? quiz.Choices;
                quiz.Updated = DateTime.UtcNow;
                quizBank.Updated = DateTime.UtcNow;
                _context.SaveChanges();
            

            return _mapper.Map<QuizBankResponse>(quizBank);
        }

        private QuizBank GetQuizBank(int id)
        {
            var quizBank = _context.QuizBanks.Find(id);
            if (quizBank == null) throw new KeyNotFoundException("Not found QuizBank");
            return quizBank;
        }
    }
}
