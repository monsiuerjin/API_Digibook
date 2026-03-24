using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using API_DigiBook.Interfaces.Services;
using API_DigiBook.Models;
using Microsoft.Extensions.Options;

namespace API_DigiBook.Services.Chat
{
    public class GeminiClient : IGeminiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ChatbotOptions _options;
        private readonly ILogger<GeminiClient> _logger;

        public GeminiClient(
            HttpClient httpClient,
            IOptions<ChatbotOptions> options,
            ILogger<GeminiClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<GeminiRecommendationResult?> GenerateRecommendationAsync(
            string query,
            IReadOnlyCollection<ChatRecommendedBook> candidates,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _options.GeminiApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Gemini API key is missing. Fallback call skipped.");
                return null;
            }

            var prompt = BuildPrompt(query, candidates);
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.GeminiModel}:generateContent?key={apiKey}";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = Math.Max(64, _options.MaxOutputTokens)
                }
            };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(endpoint, body, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Gemini request failed with status {StatusCode}: {Error}", response.StatusCode, error);
                    return null;
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var text = ExtractText(json.RootElement);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                var usage = ExtractUsage(json.RootElement);

                return new GeminiRecommendationResult
                {
                    Text = text,
                    TokenUsage = usage
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini request failed unexpectedly.");
                return null;
            }
        }

        private string BuildPrompt(string query, IReadOnlyCollection<ChatRecommendedBook> candidates)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Ban la tro ly tu van sach ngan gon, huu ich, chi dua tren du lieu metadata da cung cap.");
            sb.AppendLine("Tra loi bang tieng Viet, 6-8 cau, khong chen thong tin ngoai du lieu.");
            sb.AppendLine("Neu khong du du lieu, noi ro ly do.");
            sb.AppendLine();
            sb.AppendLine($"Yeu cau nguoi dung: {query}");
            sb.AppendLine();
            sb.AppendLine("Danh sach ung vien:");

            var index = 1;
            foreach (var book in candidates)
            {
                sb.AppendLine($"{index}. {book.Title} | Tac gia: {book.Author} | The loai: {book.Category} | Rating: {book.Rating:F1} | Gia: {book.Price:F0}");
                sb.AppendLine($"   Ly do match so bo: {book.Reason}");
                index++;
            }

            sb.AppendLine();
            sb.AppendLine("Yeu cau output: de xuat toi da 3 sach tot nhat va giai thich ngan gon vi sao hop." );
            return sb.ToString();
        }

        private static string ExtractText(JsonElement root)
        {
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var first = candidates[0];
            if (!first.TryGetProperty("content", out var content))
            {
                return string.Empty;
            }

            if (!content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            return parts[0].TryGetProperty("text", out var textElement)
                ? textElement.GetString() ?? string.Empty
                : string.Empty;
        }

        private static ChatTokenUsage ExtractUsage(JsonElement root)
        {
            if (!root.TryGetProperty("usageMetadata", out var usageMetadata))
            {
                return new ChatTokenUsage();
            }

            var prompt = usageMetadata.TryGetProperty("promptTokenCount", out var promptToken)
                ? promptToken.GetInt32()
                : 0;
            var completion = usageMetadata.TryGetProperty("candidatesTokenCount", out var completionToken)
                ? completionToken.GetInt32()
                : 0;
            var total = usageMetadata.TryGetProperty("totalTokenCount", out var totalToken)
                ? totalToken.GetInt32()
                : prompt + completion;

            return new ChatTokenUsage
            {
                PromptTokens = prompt,
                CompletionTokens = completion,
                TotalTokens = total,
                IsEstimated = false
            };
        }
    }
}
