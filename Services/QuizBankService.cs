using System.Text;
using System.Web;
using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace fuquizlearn_api.Services;

public interface IQuizBankService
{
    Task<PagedResponse<QuizBankResponse>> GetAll(PagedRequest options);
    QuizBankResponse GetById(int id);
    QuizBankResponse Create(Account currentUser, QuizBankCreate model);
    QuizBankResponse Update(int id, QuizBankUpdate model, Account currentUser);
    void Delete(int id, Account currentUser);
    void DeleteQuiz(int id, int quizId, Account account);
    QuizBankResponse UpdateQuiz(int id, int quizId, QuizUpdate model, Account account);
    QuizBankResponse AddQuiz(Account account, QuizCreate model, int id);
    QuizBankResponse Rating(int id, Account account, int rating);
    IEnumerable<QuizBankResponse> GetRelated(int id);
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
        var quizBank = GetQuizBank(id);
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
        if (model.Visibility == null) model.Visibility = Visibility.Public;

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
            throw new UnauthorizedAccessException("Unauthorized");

        _context.QuizBanks.Remove(quizBank);
        _context.SaveChanges();
    }

    public void DeleteQuiz(int id, int quizId, Account account)
    {
        var quizBank = GetQuizBank(id);
        if (account.Role != Role.Admin && account.Id != quizBank.Author.Id)
            throw new UnauthorizedAccessException("Unauthorized");

        var quiz = quizBank.Quizes.FirstOrDefault(x => x.Id == quizId);
        if (quiz == null) throw new KeyNotFoundException("Could not find the quiz");
        quizBank.Quizes.Remove(quiz);
        quizBank.Updated = DateTime.UtcNow;

        _context.QuizBanks.Update(quizBank);
        _context.SaveChanges();
    }

    public async Task<PagedResponse<QuizBankResponse>> GetAll(PagedRequest options)
    {
        var quizBanks = await _context.QuizBanks.Include(q => q.Author).ToPagedAsync(options,
            x => x.BankName.Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII),StringComparison.OrdinalIgnoreCase));
        return new PagedResponse<QuizBankResponse>
        {
            Data = _mapper.Map<IEnumerable<QuizBankResponse>>(quizBanks.Data),
            Metadata = quizBanks.Metadata
        };
    }

    public QuizBankResponse GetById(int id)
    {
        var quizBank = GetQuizBank(id);
        return _mapper.Map<QuizBankResponse>(quizBank);
    }

    public IEnumerable<QuizBankResponse> GetRelated(int id)
    {
        var tags = GetQuizBank(id).Tags;
        if(tags != null && tags.Count > 0)
        {
            var relatedQuizBanks = _context.QuizBanks.Where(qb => qb.Tags != null && qb.Tags.Any(t => tags.Contains(t))).ToList();
            return _mapper.Map<IEnumerable<QuizBankResponse>>(relatedQuizBanks);
        }
        return new List<QuizBankResponse>();
    }

    public QuizBankResponse Rating(int id, Account account, int rating)
    {
        var quizBank = GetQuizBank(id);
        if (quizBank.Rating == null) { quizBank.Rating = new List<Rating>(); }
        quizBank.Rating.RemoveAll(r => r.AccountId == account.Id);
        quizBank.Rating.Add(new Rating (account.Id, rating));
        _context.QuizBanks.Update(quizBank);
        _context.SaveChanges();

        return _mapper.Map<QuizBankResponse>(quizBank);
    }

    public QuizBankResponse Update(int id, QuizBankUpdate model, Account currentUser)
    {
        var quizBank = GetQuizBank(id);
        if (currentUser.Id != quizBank.Author.Id) throw new UnauthorizedAccessException();

        _mapper.Map(model, quizBank);
        quizBank.Updated = DateTime.UtcNow;

        _context.QuizBanks.Update(quizBank);
        _context.SaveChanges();

        return _mapper.Map<QuizBankResponse>(quizBank);
    }

    public QuizBankResponse UpdateQuiz(int id, int quizId, QuizUpdate model, Account account)
    {
        var quizBank = GetQuizBank(id);
        if (account.Id != quizBank.Author.Id) throw new UnauthorizedAccessException();

        var quiz = quizBank.Quizes.FirstOrDefault(x => x.Id == quizId);
        if (quiz == null) throw new KeyNotFoundException("Could not find the quiz");
        _mapper.Map(model, quiz);
        quiz.Updated = DateTime.UtcNow;
        quizBank.Updated = DateTime.UtcNow;
        _context.SaveChanges();


        return _mapper.Map<QuizBankResponse>(quizBank);
    }

    private QuizBank GetQuizBank(int id)
    {
        var quizBank = _context.QuizBanks.Include(i => i.Author).FirstOrDefault(i => i.Id == id);
        if (quizBank == null) throw new KeyNotFoundException("Not found QuizBank");
        return quizBank;
    }
}