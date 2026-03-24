using System.Text;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Services;
using API_DigiBook.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace API_DigiBook.Services.Chat
{
    public class ChatRecommendationService : IChatRecommendationService
    {
        private const string CatalogCacheKey = "chat:catalog:books";

        private readonly IBookRepository _bookRepository;
        private readonly IGeminiClient _geminiClient;
        private readonly IMemoryCache _memoryCache;
        private readonly ChatbotOptions _options;
        private readonly ILogger<ChatRecommendationService> _logger;

        public ChatRecommendationService(
            IBookRepository bookRepository,
            IGeminiClient geminiClient,
            IMemoryCache memoryCache,
            IOptions<ChatbotOptions> options,
            ILogger<ChatRecommendationService> logger)
        {
            _bookRepository = bookRepository;
            _geminiClient = geminiClient;
            _memoryCache = memoryCache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ChatRecommendationResponse> GetRecommendationAsync(
            ChatRecommendationRequest request,
            CancellationToken cancellationToken = default)
        {
            var normalizedQuery = Normalize(request.Query);
            var maxRecommendations = Math.Clamp(request.MaxRecommendations, 1, 5);
            var compareBookIds = request.CompareBookIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().Take(3).ToList() ?? new List<string>();

            var cacheKey = BuildCacheKey(normalizedQuery, compareBookIds, maxRecommendations);
            if (_memoryCache.TryGetValue(cacheKey, out ChatRecommendationResponse? cached) && cached != null)
            {
                cached.Cached = true;
                return cached;
            }

            List<Book> allBooks;
            try
            {
                if (!_memoryCache.TryGetValue(CatalogCacheKey, out List<Book>? cachedCatalog) || cachedCatalog == null)
                {
                    cachedCatalog = (await _bookRepository.GetAllAsync())
                        .Where(b => b.IsAvailable)
                        .ToList();

                    _memoryCache.Set(
                        CatalogCacheKey,
                        cachedCatalog,
                        TimeSpan.FromMinutes(Math.Max(2, _options.CacheMinutes)));
                }

                allBooks = cachedCatalog;
            }
            catch (Exception ex) when (IsQuotaExceeded(ex))
            {
                _logger.LogWarning(ex, "Chat recommendation throttled by upstream quota.");

                if (_memoryCache.TryGetValue(CatalogCacheKey, out List<Book>? staleCatalog) &&
                    staleCatalog != null &&
                    staleCatalog.Count > 0)
                {
                    allBooks = staleCatalog;
                }
                else
                {
                    return new ChatRecommendationResponse
                    {
                        StrategyUsed = "service-throttled",
                        Confidence = 0,
                        Answer = "He thong tam thoi vuot quota dich vu du lieu. Ban vui long thu lai sau it phut.",
                        Recommendations = new List<ChatRecommendedBook>(),
                        TokenUsage = new ChatTokenUsage()
                    };
                }
            }

            if (!allBooks.Any())
            {
                return new ChatRecommendationResponse
                {
                    StrategyUsed = "rule-retrieval",
                    Confidence = 0,
                    Answer = "Hien tai chua co du lieu sach de tu van.",
                    Recommendations = new List<ChatRecommendedBook>()
                };
            }

            var queryTokens = Tokenize(normalizedQuery);
            var scoredCandidates = allBooks
                .Select(book => new
                {
                    Book = book,
                    ScoreResult = ScoreBook(book, queryTokens)
                })
                .Where(x => x.ScoreResult.Score > 0)
                .OrderByDescending(x => x.ScoreResult.Score)
                .Take(Math.Max(_options.MaxCandidatesForPrompt, 6))
                .ToList();

            if (!scoredCandidates.Any())
            {
                scoredCandidates = allBooks
                    .OrderByDescending(b => b.Rating)
                    .ThenByDescending(b => b.ViewCount)
                    .Take(Math.Max(_options.MaxCandidatesForPrompt, 6))
                    .Select(book => new
                    {
                        Book = book,
                        ScoreResult = new ScoreResult(1.5, "Sach co danh gia cao")
                    })
                    .ToList();
            }

            var selected = scoredCandidates
                .Take(maxRecommendations)
                .Select(x => ToRecommendedBook(x.Book, x.ScoreResult.Reason))
                .ToList();

            if (compareBookIds.Count >= 2)
            {
                var compareBooks = (await _bookRepository.GetByIdsAsync(compareBookIds))
                    .Where(b => b.IsAvailable)
                    .ToList();

                if (compareBooks.Count >= 2)
                {
                    selected = compareBooks
                        .OrderByDescending(b => b.Rating)
                        .ThenBy(b => b.Price)
                        .Take(maxRecommendations)
                        .Select(b => ToRecommendedBook(b, "Duoc chon de so sanh truc tiep"))
                        .ToList();
                }
            }

            var confidence = ComputeConfidence(scoredCandidates.Select(x => x.ScoreResult.Score).ToList());
            var answer = BuildRuleBasedAnswer(normalizedQuery, selected, compareBookIds.Count >= 2);

            var response = new ChatRecommendationResponse
            {
                StrategyUsed = "rule-retrieval",
                Confidence = confidence,
                Answer = answer,
                Recommendations = selected,
                TokenUsage = EstimateTokenUsage(normalizedQuery, selected)
            };

            var shouldFallback =
                _options.EnableGeminiFallback &&
                confidence < _options.FallbackConfidenceThreshold &&
                selected.Count > 0;

            if (shouldFallback)
            {
                var geminiResult = await _geminiClient.GenerateRecommendationAsync(
                    normalizedQuery,
                    selected,
                    cancellationToken);

                if (geminiResult != null && !string.IsNullOrWhiteSpace(geminiResult.Text))
                {
                    response.StrategyUsed = "gemini-fallback";
                    response.Answer = geminiResult.Text;
                    response.TokenUsage = geminiResult.TokenUsage;
                }
            }

            _memoryCache.Set(
                cacheKey,
                response,
                TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));

            return response;
        }

        private static string Normalize(string input)
        {
            return string.IsNullOrWhiteSpace(input)
                ? string.Empty
                : input.Trim().ToLowerInvariant();
        }

        private static List<string> Tokenize(string input)
        {
            var separators = new[] { ' ', '\t', '\n', '\r', ',', '.', ';', ':', '!', '?', '-', '_', '/', '\\', '|', '"', '\'' };
            return input
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 2)
                .Distinct()
                .ToList();
        }

        private static ScoreResult ScoreBook(Book book, IReadOnlyCollection<string> queryTokens)
        {
            var score = 0.0;
            var reasons = new List<string>();

            var title = book.Title.ToLowerInvariant();
            var author = book.Author.ToLowerInvariant();
            var category = book.Category.ToLowerInvariant();
            var keywords = (book.SearchKeywords ?? new List<string>())
                .Select(k => k.ToLowerInvariant())
                .ToList();

            var titleHits = queryTokens.Count(token => title.Contains(token));
            if (titleHits > 0)
            {
                score += Math.Min(6, titleHits * 3);
                reasons.Add("Ten sach khop nhu cau");
            }

            var categoryHits = queryTokens.Count(token => category.Contains(token));
            if (categoryHits > 0)
            {
                score += Math.Min(4, categoryHits * 4);
                reasons.Add("The loai phu hop");
            }

            var authorHits = queryTokens.Count(token => author.Contains(token));
            if (authorHits > 0)
            {
                score += Math.Min(3, authorHits * 2);
                reasons.Add("Tac gia lien quan");
            }

            var keywordHits = queryTokens.Count(token => keywords.Any(k => k.Contains(token)));
            if (keywordHits > 0)
            {
                score += Math.Min(6, keywordHits * 2);
                reasons.Add("Tu khoa lien quan");
            }

            score += Math.Min(2, book.Rating / 2.5);
            score += Math.Min(2, Math.Log10(book.ViewCount + 1));

            if (book.DiscountRate > 0)
            {
                score += 0.5;
            }

            var reason = reasons.Any()
                ? string.Join(", ", reasons.Distinct())
                : "Danh gia cao va duoc quan tam";

            return new ScoreResult(score, reason);
        }

        private static double ComputeConfidence(IReadOnlyCollection<double> scores)
        {
            if (scores.Count == 0)
            {
                return 0;
            }

            var top = scores.Max();
            var avg = scores.Average();
            var confidence = (top * 0.7 + avg * 0.3) / 12.0;
            return Math.Clamp(confidence, 0, 1);
        }

        private static ChatRecommendedBook ToRecommendedBook(Book book, string reason)
        {
            return new ChatRecommendedBook
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Category = book.Category,
                Rating = book.Rating,
                Price = book.Price,
                Reason = reason
            };
        }

        private static ChatTokenUsage EstimateTokenUsage(string query, IReadOnlyCollection<ChatRecommendedBook> books)
        {
            var promptText = query + " " + string.Join(" ", books.Select(b => $"{b.Title} {b.Author} {b.Category} {b.Reason}"));
            var promptTokens = Math.Max(1, promptText.Length / 4);
            var completionTokens = 120;

            return new ChatTokenUsage
            {
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = promptTokens + completionTokens,
                IsEstimated = true
            };
        }

        private static string BuildRuleBasedAnswer(
            string query,
            IReadOnlyCollection<ChatRecommendedBook> books,
            bool isCompareMode)
        {
            if (!books.Any())
            {
                return "Minh chua tim thay sach phu hop voi mo ta nay. Ban thu bo sung the loai, muc tieu doc, hoac ten tac gia de minh loc chinh xac hon.";
            }

            var sb = new StringBuilder();
            if (isCompareMode)
            {
                sb.AppendLine("Mình da so sanh nhanh cac sach ban chon. Goi y uu tien nhu sau:");
            }
            else
            {
                sb.AppendLine("Duoi day la goi y sach phu hop voi nhu cau cua ban:");
            }

            var rank = 1;
            foreach (var book in books.Take(3))
            {
                sb.AppendLine($"{rank}. {book.Title} - {book.Author} ({book.Category}, {book.Rating:F1} sao, {book.Price:F0} VND)");
                sb.AppendLine($"   Ly do: {book.Reason}.");
                rank++;
            }

            sb.AppendLine("Neu ban muon, minh co the loc tiep theo ngan sach, do tuoi, hoac muc tieu hoc tap cu the.");
            return sb.ToString();
        }

        private static string BuildCacheKey(string query, IEnumerable<string> compareBookIds, int maxRecommendations)
        {
            var comparePart = string.Join(",", compareBookIds.OrderBy(x => x));
            return $"chat:{query}:{comparePart}:{maxRecommendations}";
        }

        private static bool IsQuotaExceeded(Exception ex)
        {
            return ex.Message.Contains("ResourceExhausted", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("Quota exceeded", StringComparison.OrdinalIgnoreCase);
        }

        private readonly record struct ScoreResult(double Score, string Reason);
    }
}
