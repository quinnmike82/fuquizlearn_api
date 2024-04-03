using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Gemeni;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Newtonsoft.Json;
using AIRequest = fuquizlearn_api.Models.Request;
using Content = fuquizlearn_api.Models.Gemeni.Content;
using Part = fuquizlearn_api.Models.Gemeni.Part;

namespace fuquizlearn_api.Services
{
    public interface IGeminiAIService
    {
        Task<GeminiAiResponse> GetTextOnly(QuizCreate prompt, CancellationToken cancellationToken = default);

        Task<GeminiAiResponse> GetTextAndImage(Stream file, string prompt,
            CancellationToken cancellationToken = default);
        Task<GeminiAiResponse> CheckCorrectAnswer(QuizCreate prompt, CancellationToken cancellationToken = default);
        Task<GeminiAiResponse> GetAnwser(QuizCreate prompt, CancellationToken cancellationToken = default);
        Task<EmbedResponse?> GetEmbedding(IEnumerable<string> textStrings, CancellationToken cancellationToken = default);
    }

    public class GeminiService : IGeminiAIService
    {
        private readonly AppSettings _appSettings;
        private readonly HttpClient _httpTextOnlyClient;
        private readonly HttpClient _httpTextAndImageClient;
        private readonly HttpClient _httpEmbeddingClient;   

        public GeminiService(AppSettings appSettings, IHttpClientFactory httpClientFactory)
        {
            _appSettings = appSettings;
            _httpTextOnlyClient = httpClientFactory.CreateClient("GeminiAITextOnly");
            _httpTextAndImageClient = httpClientFactory.CreateClient("GeminiAITextAndImage");
            _httpEmbeddingClient = httpClientFactory.CreateClient("Gemini Embedding");
        }

        public async Task<GeminiAiResponse> GetTextAndImage(Stream file, string prompt,
            CancellationToken cancellationToken = default)
        {
            byte[] data;
            await using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                data = memoryStream.ToArray();
            }

            var response = await _httpTextAndImageClient.PostAsJsonAsync($"?key={_appSettings.GeminiAIApiKey}",
                GetTextAndImageRequest(data, prompt), cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            var geminiAiResponse = JsonConvert.DeserializeObject<GeminiAiResponse>(responseContent);
            return geminiAiResponse;
        }

        public async Task<GeminiAiResponse> GetTextOnly(QuizCreate prompt, CancellationToken cancellationToken)
        {
            string aiPrompt = prompt.Question + "\nAnwser is \n" + prompt.Answer + "\n Explain the question with max 60 words(refer to the question language)";
            return await GetAIResponse(aiPrompt, cancellationToken);
        }

        public async Task<GeminiAiResponse> CheckCorrectAnswer(QuizCreate prompt, CancellationToken cancellationToken)
        {
            string aiPrompt = "Question and choices are: \n" + prompt.Question + "\nWhat is the right anwser? no need to explain, just the answer(refer to the question language)";
            return await GetAIResponse(aiPrompt, cancellationToken);
        }

        public async Task<GeminiAiResponse> GetAnwser(QuizCreate prompt, CancellationToken cancellationToken)
        {
            string aiPrompt = "Question is: \n" + prompt.Question + "\nWhat is the right anwser? no need to explain, just the answer(refer to the question language)";
            return await GetAIResponse(aiPrompt, cancellationToken);
        }

        public async Task<EmbedResponse?> GetEmbedding(IEnumerable<string> textStrings, CancellationToken cancellationToken = default)
        {
            var request = new EmbedContentRequest
            {
                TaskType = TaskType.SEMANTIC_SIMILARITY,
                Title = "Embedding",
                Content = new Content()
                {
                    Parts = textStrings.Select(text => new Part
                    {
                        Text = text
                    }).ToArray()
                }
            };
            
            var response = await _httpEmbeddingClient.PostAsJsonAsync($"?key={_appSettings.GeminiAIApiKey}", request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            var responseContent = await response.Content.ReadAsStringAsync();
            var embedResponse = JsonConvert.DeserializeObject<EmbedResponse>(responseContent);
            return embedResponse;
        }

        public async Task<EmbedResponse?> GetEmbedding(string text, CancellationToken cancellationToken = default)
        {
            var request = new EmbedContentRequest
            {
                TaskType = TaskType.SEMANTIC_SIMILARITY,
                Title = "Embedding",
                Content = new Content()
                {
                    Parts = new[]
                    {
                        new Part()
                        {
                            Text = text
                        }
                    }
                }
            };

            var response = await _httpEmbeddingClient.PostAsJsonAsync($"?key={ _appSettings.GeminiAIApiKey}", request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null; 
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var embedResponse = JsonConvert.DeserializeObject<EmbedResponse>(responseContent);
            return embedResponse;
        }

        public async Task<GeminiAiResponse> GetAIResponse(string prompt, CancellationToken cancellationToken)
        {
            var response = await _httpTextOnlyClient.PostAsJsonAsync($"?key={_appSettings.GeminiAIApiKey}",
                GetTextOnlyRequest(prompt), cancellationToken);
            await Console.Out.WriteLineAsync(response.ToString());
            if (!response.IsSuccessStatusCode) return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            var geminiAiResponse = JsonConvert.DeserializeObject<GeminiAiResponse>(responseContent);
            return geminiAiResponse;
        }


        private GeminiAiRequest GetTextAndImageRequest(byte[] data, string prompt)
        {
            var request = new GeminiAiRequest
            {
                contents = new List<AIRequest.Content>
                {
                    new AIRequest.Content
                    {
                        parts = new List<AIRequest.Part>
                        {
                            new AIRequest.Part
                            {
                                text = prompt,
                            },
                            new AIRequest.Part
                            {
                                inline_data = new InlineData
                                {
                                    mime_type = "image/jpeg",
                                    data = data
                                }
                            }
                        }
                    }
                }
            };
            return request;
        }


        private GeminiAiRequest GetTextOnlyRequest(string prompt)
        {
            var request = new GeminiAiRequest
            {
                contents = new List<AIRequest.Content>
                {
                    new AIRequest.Content
                    {
                        parts = new List<AIRequest.Part>
                        {
                            new AIRequest.Part
                            {
                                text = prompt,
                            }
                        }
                    }
                }
            };
            return request;
        }
    }
};