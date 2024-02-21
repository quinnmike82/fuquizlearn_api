using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Newtonsoft.Json;
using AIRequest = fuquizlearn_api.Models.Request;

namespace fuquizlearn_api.Services
{
    public interface IGeminiAIService
    {
        Task<GeminiAiResponse> GetTextOnly(string prompt, CancellationToken cancellationToken = default);

        Task<GeminiAiResponse> GetTextAndImage(Stream file, string prompt,
            CancellationToken cancellationToken = default);
    }

    public class GeminiService : IGeminiAIService
    {
        private readonly AppSettings _appSettings;
        private readonly HttpClient _httpTextOnlyClient;
        private readonly HttpClient _httpTextAndImageClient;

        public GeminiService(AppSettings appSettings, IHttpClientFactory httpClientFactory)
        {
            _appSettings = appSettings;
            _httpTextOnlyClient = httpClientFactory.CreateClient("GeminiAITextOnly");
            _httpTextAndImageClient = httpClientFactory.CreateClient("GeminiAITextAndImage");
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

        public async Task<GeminiAiResponse> GetTextOnly(string prompt, CancellationToken cancellationToken)
        {
            Console.WriteLine("Hello World! " + _appSettings.GeminiAIApiKey);
            var response = await _httpTextOnlyClient.PostAsJsonAsync($"?key={_appSettings.GeminiAIApiKey}",
                GetTextOnlyRequest(prompt), cancellationToken);
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