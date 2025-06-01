using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace AichatBot3.Service
{
    public class ImageGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _huggingFaceApiKey;
        private static readonly Random _random = new Random();

        // --- Define the three working model IDs ---
        private const string Model1_ID = "black-forest-labs/FLUX.1-dev";
        private const string Model2_ID = "stabilityai/stable-diffusion-3.5-large"; // Note: SD3 models might have specific API needs or support limitations.
        private const string Model3_ID = "stabilityai/stable-diffusion-xl-base-1.0";
        // -----------------------------------------

        public ImageGenerationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _huggingFaceApiKey = configuration["HuggingFace:ApiToken"];

            if (string.IsNullOrWhiteSpace(_huggingFaceApiKey))
            {
                throw new InvalidOperationException("HuggingFace API Token ('HuggingFace:ApiToken') is missing or empty in configuration.");
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _huggingFaceApiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(200);
        }

        public async Task<(string ImageDataUri, double GenerationTimeSeconds)> GenerateImageAsync(string prompt, string modelIdentifier) // e.g., modelIdentifier = "Model1", "Model2", "Model3"
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
            }

            int seed = _random.Next(1, int.MaxValue);
            var startTime = DateTime.UtcNow;

            var requestBody = new
            {
                inputs = prompt,
                parameters = new
                {
                    num_inference_steps = 30,
                    guidance_scale = 7.5,
                    seed
                }
                // NOTE: FLUX or SD3 might benefit from/require different parameters here.
                // Check their specific documentation if you get suboptimal results.
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            // --- Updated Model Selection Logic ---
            string selectedModelId;
            Console.WriteLine($"DEBUG: Received model identifier parameter: '{modelIdentifier}'"); // Log input

            switch (modelIdentifier?.Trim()) // Use Trim() for safety, handle null
            {
                case "Model1":
                    selectedModelId = Model1_ID;
                    break;
                case "Model2":
                    selectedModelId = Model2_ID;
                    break;
                case "Model3":
                    selectedModelId = Model3_ID;
                    break;
                default:
                    // Fallback to a likely reliable model if identifier is unknown or null
                    Console.WriteLine($"WARN: Unknown model identifier '{modelIdentifier}'. Defaulting to Model3.");
                    selectedModelId = Model3_ID;
                    modelIdentifier = "Model3"; // Update identifier for logging consistency
                    break;
            }
            // -------------------------------------

            string apiUrl = $"https://api-inference.huggingface.co/models/{selectedModelId}";

            Console.WriteLine($"[{DateTime.UtcNow:O}] Attempting image generation...");
            Console.WriteLine($"  Model ID: {selectedModelId} (Selected based on input: '{modelIdentifier}')");
            Console.WriteLine($"  Prompt: {prompt}");
            Console.WriteLine($"  API URL: {apiUrl}");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    // ... (Keep the existing detailed error handling logic here) ...
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) - Response Body: {responseContent}");
                    string structuredErrorMessage = $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
                    try { /* ... parse error ... */ } catch (JsonException) { /* ... fallback ... */ }
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && structuredErrorMessage.Contains("currently loading")) { /* ... handle loading ... */ }
                    throw new Exception($"API Error: {structuredErrorMessage}");
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    Console.WriteLine("API returned success status code but response body was empty.");
                    throw new Exception("Received empty successful response from image generation API.");
                }

                double generationTime = (DateTime.UtcNow - startTime).TotalSeconds;
                Console.WriteLine($"Image generated successfully using {selectedModelId} in {generationTime:F2} seconds. Size: {imageBytes.Length} bytes.");

                return ($"data:image/png;base64,{Convert.ToBase64String(imageBytes)}", generationTime);
            }
            catch (TaskCanceledException ex)
            {
                // ... (Keep existing timeout handling) ...
                Console.WriteLine($"Request timeout after {_httpClient.Timeout.TotalSeconds} seconds: {ex.Message}");
                throw new Exception($"Image generation request timed out ({_httpClient.Timeout.TotalSeconds}s). The model might be too slow or the server busy. Try again later.");
            }
            catch (HttpRequestException ex)
            {
                // ... (Keep existing network error handling) ...
                Console.WriteLine($"Network error: {ex.Message}");
                string innerExceptionMessage = ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}" : "";
                throw new Exception($"Network error communicating with Hugging Face API.{innerExceptionMessage} Check connection and API status.");
            }
            catch (Exception ex)
            {
                // ... (Keep existing general error handling) ...
                Console.WriteLine($"Error during image generation: {ex.Message}");
                throw new Exception($"Failed to generate image. {ex.Message}");
            }
        }
    }
}
