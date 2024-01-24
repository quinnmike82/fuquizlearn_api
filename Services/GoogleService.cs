using Newtonsoft.Json;

namespace fuquizlearn_api.Services
{
    public interface IGoogleService
    {
        Task<string?> GetEmailByToken(string token);
    }
    public class GoogleService : IGoogleService

    {
        private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo?access_token={0}";

        public async Task<string?> GetEmailByToken(string token)
        {

            try
            {
                using HttpClient client = new();

                string url = string.Format(UserInfoEndpoint, token);


                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Use Newtonsoft.Json to parse the JSON response
                    dynamic jsonObject = JsonConvert.DeserializeObject(jsonResponse);

                    // Access email property based on actual location within JSON object
                    string email = jsonObject.email;

                    return email;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return null;
            }
        }
    }
}
