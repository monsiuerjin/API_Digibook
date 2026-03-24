using System.Text.Json.Serialization;

namespace API_DigiBook.Models
{
    public class ChatRecommendationRequest
    {
        public string Query { get; set; } = string.Empty;
        public List<string>? CompareBookIds { get; set; }
        public int MaxRecommendations { get; set; } = 5;
    }

    public class ChatRecommendationResponse
    {
        public string StrategyUsed { get; set; } = "rule-retrieval";
        public double Confidence { get; set; }
        public bool Cached { get; set; }
        public string Answer { get; set; } = string.Empty;
        public List<ChatRecommendedBook> Recommendations { get; set; } = new();
        public ChatTokenUsage TokenUsage { get; set; } = new();
    }

    public class ChatRecommendedBook
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Rating { get; set; }
        public double Price { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ChatTokenUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public bool IsEstimated { get; set; } = true;
    }

    public class GeminiRecommendationResult
    {
        public string Text { get; set; } = string.Empty;
        public ChatTokenUsage TokenUsage { get; set; } = new();
    }

    public class ChatbotOptions
    {
        public bool Enabled { get; set; } = true;
        public bool EnableGeminiFallback { get; set; } = true;
        public string GeminiApiKey { get; set; } = string.Empty;
        public string GeminiModel { get; set; } = "gemini-1.5-flash";
        public double FallbackConfidenceThreshold { get; set; } = 0.65;
        public int MaxCandidatesForPrompt { get; set; } = 6;
        public int MaxOutputTokens { get; set; } = 300;
        public int CacheMinutes { get; set; } = 20;
        public int MaxPromptTokens { get; set; } = 2000;
    }
}
