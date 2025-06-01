using System.Text.Json;
using System.Text;

namespace AichatBot3.Service
{
    public class ChatGptService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ChatGptService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60); // Increase timeout to 60 seconds
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        public async Task<string> GetChatResponse(string userMessage)
        {
            try
            {
                var requestBody = new
                {
                    model = "google/gemma-3-1b-it:free",
                    messages = new[]
                    {
                    new { role = "system", content = "You are a intelligent Ai assistant.dont use *" },
                    new { role = "user", content = userMessage }
                },
                    max_tokens = 1000,  // Controls response length
                    temperature = 0.7  // Controls randomness
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", jsonContent);

                // Get response content for better error handling
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API Error: {response.StatusCode}, Content: {responseContent}");
                    return $"Error: Unable to get response. Status code: {response.StatusCode}";
                }

                try
                {
                    using var jsonDoc = JsonDocument.Parse(responseContent);
                    return jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"JSON parsing error: {ex.Message}, Response: {responseContent}");
                    return "Error: Unable to parse response from API.";
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request error: {ex.Message}");
                return "Error: Network issue while connecting to AI service.";
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timed out");
                return "Error: Request timed out. Please try again.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return "Error: An unexpected error occurred. Please try again later.";
            }
        }
    }
}
