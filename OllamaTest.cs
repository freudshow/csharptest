using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace ConsoleApp1
{
    public class OllamaService
    {
        private static readonly HttpClient client = new HttpClient();
        private const string OllamaUrl = "http://localhost:11434/v1";
        private const string _ModelName = "gemma4:e4b"; // Change this to your model
        public static string ModelName { get { return _ModelName; } }

        public static async Task<string> GenerateContent(string prompt)
        {
            // 1. Define the Request Payload (JSON structure required by Ollama)
            var payload = new
            {
                model = _ModelName,
                prompt = prompt,
                options = new { temperature = 0.7 }
            };

            // 2. Send the POST request
            try
            {
                var content = JsonContent.Create(payload);
                HttpResponseMessage response = await client.PostAsync(OllamaUrl, content);

                // Check for connection errors or bad status codes
                if (!response.IsSuccessStatusCode)
                {
                    return $"Error connecting to Ollama. Ensure Ollama is running and the model '{_ModelName}' is pulled. Status Code: {response.StatusCode}";
                }

                // 3. Read and Deserialize the Response
                var jsonResponse = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    // Ollama wraps the response in a 'response' field
                    return doc.RootElement.GetProperty("response").GetString();
                }
            }
            catch (HttpRequestException ex)
            {
                return $"Connection Error: Could not reach Ollama. Is it running? Details: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"An unexpected error occurred: {ex.Message}";
            }
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            string userPrompt = "Write a short, encouraging poem about learning a new programming language.";

            Console.WriteLine($"--- Sending prompt to Ollama using {OllamaService.ModelName} ---");

            string result = await OllamaService.GenerateContent(userPrompt);

            Console.WriteLine("\n=========================================");
            Console.WriteLine("🤖 Generated Response:");
            Console.WriteLine(result);
            Console.WriteLine("=========================================");
            Console.ReadLine();
        }
    }
}