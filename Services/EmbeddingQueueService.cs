using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Gemeni;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.QuizBank;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace fuquizlearn_api.Services;

public interface IEmbeddingQueueService
{
    void ProcessQueue(int quizBankId);
}

public class EmbeddingQueueService : IEmbeddingQueueService
{
    // private readonly IQuizService _quizService;
    private readonly ILogger<EmbeddingQueueService> _logger;

    // private readonly IQuizBankService _quizBankService;
    // private readonly IGeminiAIService _geminiAIService;
    private readonly IServiceProvider _serviceProvider;


    public EmbeddingQueueService(
        ILogger<EmbeddingQueueService> logger,
        IServiceProvider serviceProvider )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async void ProcessQueue(int quizBankId)
    {
        _logger.LogDebug("Processing embedding queue for quizbank {quizBankId}", quizBankId);
        // Process the queue
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var _quizBankService = scope.ServiceProvider.GetRequiredService<IQuizBankService>();
            var _quizService = scope.ServiceProvider.GetRequiredService<IQuizService>();
            var _geminiAIService = scope.ServiceProvider.GetRequiredService<IGeminiAIService>();

            var quizbank = _quizBankService.GetById(quizBankId);
            var quizzes = _quizService.GetAll(quizBankId);
            var quizbankEmbeddingReponse = await EmbeddingQuizbank(quizbank, quizzes, _geminiAIService);

            if (quizbankEmbeddingReponse != null)
            {
                var quizBankUpdate = new QuizBankUpdate
                {
                    Embedding = new Vector(quizbankEmbeddingReponse.Embedding.Values)
                };

                // Do update
                var updateQuizBank = _quizBankService.Update(quizBankId, quizBankUpdate);
                _logger.LogDebug("Embedding for quizbank {quizBankId} processed successfully", quizBankId);
            }

            foreach (var quiz in quizzes)
            {
                var quizResponse = await EmbeddingQuiz(quiz, _geminiAIService);
                if (quizResponse == null) continue;
                var quizUpdate = new QuizUpdate
                {
                    Embedding = new Vector(quizResponse.Embedding.Values)
                };
                // Do update

                var updateQuiz = _quizService.UpdateQuiz(quiz.Id, quizUpdate);
                _logger.LogDebug("Embedding for quiz {quizId} that belong to {quizbankId} processed successfully", quiz.Id,quizBankId);
            }


            _logger.LogDebug("Embedding queue for quizbank {quizBankId} processed successfully", quizBankId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing embedding queue for quiz bank {quizBankId}", quizBankId);
            // End
        }
    }

    private async Task<EmbedResponse?> EmbeddingQuizbank(QuizBankResponse quizBank, IEnumerable<Quiz> quizzes,
        IGeminiAIService _geminiAIService)
    {
        var content = quizBank.BankName + " ; " + quizBank.Description;
        var quizzContent = string.Join(" ; ", quizzes.Select(q => q.Question + " ; " + q.Answer));

        var textStrings = new List<string>
        {
            content,
            quizzContent
        };

        var embedding = await _geminiAIService.GetEmbedding(textStrings);
        return embedding;
    }

    private async Task<EmbedResponse?> EmbeddingQuiz(Quiz quiz, IGeminiAIService _geminiAIService)
    {
        var content = quiz.Question + " ; " + quiz.Answer;
        var embedding = await _geminiAIService.GetEmbedding(
            new List<string>()
            {
                content
            });

        return embedding;
    }
}