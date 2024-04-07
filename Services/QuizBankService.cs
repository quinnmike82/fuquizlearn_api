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
using System.Text;
using System.Web;
using Hangfire;
using Microsoft.EntityFrameworkCore.Internal;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace fuquizlearn_api.Services;

public interface IQuizBankService
{
    Task<PagedResponse<QuizBankResponse>> GetAll(PagedRequest options);
    QuizBankResponse GetById(int id);
    Task<QuizBankResponse> Create(Account currentUser, QuizBankCreate model);
    QuizBankResponse Update(int id, QuizBankUpdate model, Account currentUser);
    QuizBankResponse Update(int id, QuizBankUpdate model);
    void Delete(int id, Account currentUser);
    void DeleteQuiz(int id, int quizId, Account account);
    QuizBankResponse UpdateQuiz(int id, int quizId, QuizUpdate model, Account account);
    QuizBankResponse AddQuiz(Account account, QuizCreate model, int id);
    QuizBankResponse Rating(int id, Account account, int rating);
    IEnumerable<QuizBankResponse> GetRelated(int id);
    Task<PagedResponse<QuizBankResponse>> GetMy(PagedRequest options, Account account);
    Task<ProgressResponse> SaveProgress(int quizbankId, Account account, SaveProgressRequest saveProgressRequest);
    Task<ProgressResponse> GetProgress(int quizbankId, Account account);
    Task<QuizBankResponse> CopyQuizBank(string newQuizBankName, int quizbankId, Account account);
    Task<PagedResponse<QuizBankResponse>> GetBySubject(PagedRequest options, string tag, Account account);
}

public class QuizBankService : IQuizBankService
{
    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IGeminiAIService _geminiAiService;
    private readonly IQuizService _quizService;


    public QuizBankService(DataContext context, IMapper mapper, IGeminiAIService geminiAiService
    )
    {
        _context = context;
        _mapper = mapper;
        _geminiAiService = geminiAiService;
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

    public async Task<QuizBankResponse> CopyQuizBank(string newQuizBankName, int quizbankId, Account account)
    {
        var quizBank = GetQuizBank(quizbankId);
        var newName = newQuizBankName.Trim() != "" ? newQuizBankName : quizBank.BankName;
        var newBank = new QuizBank
        {
            BankName = newName,
            Description = quizBank.Description,
            Visibility = quizBank.Visibility,
            Author = account,
            Tags = quizBank.Tags,
            Quizes = new List<Quiz>()
        };
        foreach (var quiz in quizBank.Quizes)
        {
            var newQuiz = new QuizCreate
            {
                Answer = quiz.Answer,
                Explaination = quiz.Explaination,
                Question = quiz.Question
            };
            newBank.Quizes.Add(_mapper.Map<Quiz>(newQuiz));
        }

        _context.QuizBanks.Add(newBank);
        await _context.SaveChangesAsync();

        return _mapper.Map<QuizBankResponse>(newBank);
    }

    public async Task<QuizBankResponse> Create(Account currentUser, QuizBankCreate model)
    {
        model.Visibility ??= Visibility.Public;

        var quizBank = _mapper.Map<QuizBank>(model);
        quizBank.Created = DateTime.UtcNow;
        quizBank.Author = currentUser;

        _context.QuizBanks.Add(quizBank);
        await _context.SaveChangesAsync();

        BackgroundJob.Enqueue<IEmbeddingQueueService>(x => x.ProcessQueue(quizBank.Id)); // do embedding after 30s

        return _mapper.Map<QuizBankResponse>(quizBank);
    }


    public QuizBankResponse Update(int id, QuizBankUpdate model)
    {
        var quizBank = GetQuizBank(id);
        _mapper.Map(model, quizBank);
        quizBank.Updated = DateTime.UtcNow;

        _context.QuizBanks.Update(quizBank);
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
        var quizBanks = await _context.QuizBanks.Include(c => c.Quizes).Include(c => c.Author).Include(q => q.Quizes)
            .ToPagedAsync(options,
                x => x.BankName.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
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

    public async Task<PagedResponse<QuizBankResponse>> GetBySubject(PagedRequest options, string tag, Account account)
    {
        var quizBanks = await _context.QuizBanks.Include(c => c.Quizes)
            .Include(c => c.Author)
            .Where(q => q.Tags != null && q.Tags.Contains(tag))
            .ToPagedAsync(options,
                x => x.BankName.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
        return new PagedResponse<QuizBankResponse>
        {
            Data = _mapper.Map<IEnumerable<QuizBankResponse>>(quizBanks.Data),
            Metadata = quizBanks.Metadata
        };
    }

    public async Task<PagedResponse<QuizBankResponse>> GetMy(PagedRequest options, Account account)
    {
        var quizBanks = await _context.QuizBanks.Include(c => c.Quizes).Include(c => c.Author)
            .Where(qb => qb.Author.Id == account.Id).ToPagedAsync(options,
                x => x.BankName.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
        return new PagedResponse<QuizBankResponse>
        {
            Data = _mapper.Map<IEnumerable<QuizBankResponse>>(quizBanks.Data),
            Metadata = quizBanks.Metadata
        };
    }

    public async Task<ProgressResponse> GetProgress(int quizbankId, Account account)
    {
        GetQuizBank(quizbankId);
        var progress =
            await _context.LearnedProgress.FirstOrDefaultAsync(p =>
                p.QuizBankId == quizbankId && p.AccountId == account.Id);
        if (progress == null)
        {
            throw new KeyNotFoundException("Not found progress");
        }

        return _mapper.Map<ProgressResponse>(progress);
    }

    public IEnumerable<QuizBankResponse> GetRelated(int id)
    {
        var relatedQuizBanks = HybridRecommendation(id, 1, 2, 70, 4);
        return _mapper.Map<IEnumerable<QuizBankResponse>>(relatedQuizBanks);
    }

    private IEnumerable<QuizBank> GetRelatedByEmbedding(int id)
    {
        var embedding = GetQuizBank(id).Embedding;
        if (embedding == null) return new List<QuizBank>();

        var relatedQuizBanks = _context.QuizBanks
            .Where(qb => qb.Embedding != null && qb.Id != id).Include(quizBank => quizBank.Embedding!)
            .AsEnumerable()
            .OrderBy(qb => qb.Embedding!.CosineDistance(embedding))
            .Take(10).ToList();

        return relatedQuizBanks;
    }

    /**
     * Hybrid recommendation system
     *
     * @param id quizbank id
     *
     */
    private IEnumerable<QuizBank> HybridRecommendation(int id, float tagWeight = 1, float sematicWeight = 1,
        int rrFK = 50, int take = 10)
    {
        var tags = GetQuizBank(id).Tags ?? new List<string>();
        var embedding = GetQuizBank(id).Embedding ?? new Vector(new float[768]);


        var tagRelatedQuizBanks = _context.QuizBanks.Include(q => q.Author).Include(q => q.Quizes)
            .Where(qb => qb.Tags != null && qb.Tags.Any(t => tags.Contains(t))).Take(take)
            .Include(qb => qb.Author)
            .Select(qb => new
            {
                QuizBank = qb,
                Score = 1.0 / (rrFK + tags.IndexOf(qb.Tags!.First())) * tagWeight
            })
            .ToList();
        var embeddingRelatedQuizBanks = _context.QuizBanks
            .Where(qb => qb.Embedding != null && qb.Id != id)
            .OrderBy(qb => qb.Embedding!.CosineDistance(embedding))
            .Include(qb => qb.Author)
            .Include(qb => qb.Quizes)
            .Take(take)
            .Select(qb => new
            {
                QuizBank = qb,
                // score by distance
                Score = 1.0 / (rrFK +( qb.Embedding != null ? qb.Embedding!.CosineDistance(embedding) : 1)) * sematicWeight
            })
            .ToList();
        var relatedQuizBanks = tagRelatedQuizBanks.Concat(embeddingRelatedQuizBanks).GroupBy(x => x.QuizBank)
            .Select(x => new
            {
                QuizBank = x.Key,
                Score = x.Sum(s => s.Score)
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.QuizBank)
            .Take(take)
            .ToList();

        return relatedQuizBanks;
    }

    public QuizBankResponse Rating(int id, Account account, int rating)
    {
        var quizBank = GetQuizBank(id);
        if (quizBank.Rating == null)
        {
            quizBank.Rating = new List<Rating>();
        }

        quizBank.Rating.RemoveAll(r => r.AccountId == account.Id);
        quizBank.Rating.Add(new Rating(account.Id, rating));
        _context.QuizBanks.Update(quizBank);
        _context.SaveChanges();

        return _mapper.Map<QuizBankResponse>(quizBank);
    }

    public async Task<ProgressResponse> SaveProgress(int quizbankId, Account account,
        SaveProgressRequest saveProgressRequest)
    {
        GetQuizBank(quizbankId);
        var progress =
            await _context.LearnedProgress.FirstOrDefaultAsync(p =>
                p.QuizBankId == quizbankId && p.AccountId == account.Id);
        if (progress == null)
        {
            progress = new LearnedProgress
            {
                AccountId = account.Id,
                QuizBankId = quizbankId,
                CurrentQuizId = saveProgressRequest.CurrentQuizId,
                LearnedQuizIds = saveProgressRequest.LearnedQuizIds,
                LearnMode = saveProgressRequest.LearnMode,
                Created = DateTime.UtcNow,
            };
            await _context.LearnedProgress.AddAsync(progress);
        }
        else
        {
            progress.CurrentQuizId = saveProgressRequest.CurrentQuizId;
            progress.LearnedQuizIds = saveProgressRequest.LearnedQuizIds;
            progress.LearnMode = saveProgressRequest.LearnMode;
            _context.LearnedProgress.Update(progress);
        }

        await _context.SaveChangesAsync();
        return _mapper.Map<ProgressResponse>(progress);
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
BackgroundJob.Enqueue<IEmbeddingQueueService>(x => x.ProcessQueue(quizBank.Id)); 
        return _mapper.Map<QuizBankResponse>(quizBank);
    }

    private QuizBank GetQuizBank(int id)
    {
        var quizBank = _context.QuizBanks.Include(i => i.Author).Include(q => q.Quizes).FirstOrDefault(i => i.Id == id);
        if (quizBank == null) throw new KeyNotFoundException("Not found QuizBank");
        return quizBank;
    }
}