using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Singleton;
using API_DigiBook.Services;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILogger<BooksController> _logger;
        private readonly LoggerService _systemLogger;
        private readonly ICacheService _cache;

        public BooksController(IBookRepository bookRepository, ILogger<BooksController> logger, ICacheService cache)
        {
            _bookRepository = bookRepository;
            _logger = logger;
            _systemLogger = LoggerService.Instance; 
            _cache = cache;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var count = await _bookRepository.CountAsync();
                return Ok(new { success = true, message = "Firebase connection successful!", booksCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Firebase connection");
                return StatusCode(500, new { success = false, message = "Firebase connection failed", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBooks()
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey("books:all");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var all = await _bookRepository.GetAllAsync();
                    return all.ToList();
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books");
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("isbn/{isbn}")]
        public async Task<IActionResult> GetBookByIsbn(string isbn)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:isbn:{isbn}");
                var book = await _cache.GetOrSetAsync(cacheKey, () => _bookRepository.GetByIsbnAsync(isbn));

                if (book == null)
                {
                    return NotFound(new { success = false, message = $"Book with ISBN '{isbn}' not found" });
                }

                return Ok(new { success = true, data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by ISBN: {Isbn}", isbn);
                return StatusCode(500, new { success = false, message = "Error retrieving book", error = ex.Message });
            }
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBookBySlug(string slug)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:slug:{slug}");
                var book = await _cache.GetOrSetAsync(cacheKey, () => _bookRepository.GetBySlugAsync(slug));

                if (book == null)
                {
                    return NotFound(new { success = false, message = $"Book with slug '{slug}' not found" });
                }

                // Note: Incrementing views happens here for backward compatibility with direct slug calls
                await _bookRepository.IncrementViewCountAsync(book.Id);
                _cache.BumpVersion("books");
                return Ok(new { success = true, data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by slug: {Slug}", slug);
                return StatusCode(500, new { success = false, message = "Error retrieving book", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBook([FromBody] Book book)
        {
            try
            {
                book.CreatedAt = Timestamp.GetCurrentTimestamp();
                book.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var bookId = await _bookRepository.AddAsync(book, book.Id);
                book.Id = bookId;

                _cache.BumpVersion("books");

                return CreatedAtAction(nameof(GetBookByIsbn), new { isbn = book.Isbn }, new { success = true, message = "Book created successfully", data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return StatusCode(500, new { success = false, message = "Error creating book", error = ex.Message });
            }
        }

        [HttpPut("isbn/{isbn}")]
        public async Task<IActionResult> UpdateBook(string isbn, [FromBody] Book book)
        {
            try
            {
                book.Isbn = isbn;
                book.UpdatedAt = Timestamp.GetCurrentTimestamp();
                var updated = await _bookRepository.UpdateByIsbnAsync(isbn, book);

                if (!updated)
                {
                    return NotFound(new { success = false, message = $"Book with ISBN '{isbn}' not found" });
                }

                _cache.BumpVersion("books");
                return Ok(new { success = true, message = "Book updated successfully", data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book with ISBN: {Isbn}", isbn);
                return StatusCode(500, new { success = false, message = "Error updating book", error = ex.Message });
            }
        }

        [HttpPost("by-ids")]
        public async Task<IActionResult> GetBooksByIds([FromBody] JsonElement body)
        {
            try
            {
                var bookIds = ExtractBookIds(body);
                if (bookIds == null || !bookIds.Any()) return BadRequest(new { success = false, message = "Book IDs are required" });

                var cacheKey = _cache.GetVersionedKey($"books:batch:{string.Join(",", bookIds.OrderBy(x => x))}");
                var books = await _cache.GetOrSetAsync(cacheKey, () => _bookRepository.GetByIdsAsync(bookIds));

                return Ok(new { success = true, count = books?.Count() ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by IDs");
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("related")]
        public async Task<IActionResult> GetRelatedBooks(
            [FromQuery] string category, 
            [FromQuery] string currentBookId, 
            [FromQuery] string? author = null, 
            [FromQuery] int limit = 5)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:related:{currentBookId}:{category}:{author}:{limit}");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var all = await _bookRepository.GetAllAsync();
                    var related = all.Where(b => b.Id != currentBookId && 
                        (string.Equals(b.Category, category, StringComparison.OrdinalIgnoreCase) || 
                         (!string.IsNullOrEmpty(author) && string.Equals(b.Author, author, StringComparison.OrdinalIgnoreCase))))
                        .Take(limit)
                        .ToList();
                    return related;
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related books for: {Id}", currentBookId);
                return StatusCode(500, new { success = false, message = "Error retrieving related books", error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title)) return BadRequest(new { success = false, message = "Search title is required" });
                
                var books = await _bookRepository.SearchByTitleAsync(title);
                return Ok(new { success = true, count = books.Count(), data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching books by title: {Title}", title);
                return StatusCode(500, new { success = false, message = "Error searching books", error = ex.Message });
            }
        }

        [HttpGet("top-rated")]
        public async Task<IActionResult> GetTopRatedBooks([FromQuery] int count = 10)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:top-rated:{count}");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var result = await _bookRepository.GetTopRatedAsync(count);
                    return result.ToList();
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top rated books");
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("paginated")]
        public async Task<IActionResult> GetBooksPaginated(
            [FromQuery] string? limit = null,
            [FromQuery] string? offset = null,
            [FromQuery] string? category = null,
            [FromQuery] string? sortBy = null)
        {
            try
            {
                int limitValue = int.TryParse(limit, out var l) && l > 0 ? l : 10;
                int offsetValue = int.TryParse(offset, out var o) && o >= 0 ? o : 0;
                var sortValue = sortBy ?? "newest";

                var cacheKey = _cache.GetVersionedKey($"books:paginated:{category ?? "all"}:{sortValue}:{offsetValue}:{limitValue}");
                var page = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var allBooks = await _bookRepository.GetAllAsync();
                    var filtered = string.IsNullOrWhiteSpace(category) ? allBooks : allBooks.Where(b => string.Equals(b.Category, category, StringComparison.OrdinalIgnoreCase));
                    var sorted = sortValue switch
                    {
                        "price_asc" => filtered.OrderBy(b => b.Price),
                        "price_desc" => filtered.OrderByDescending(b => b.Price),
                        "rating" => filtered.OrderByDescending(b => b.Rating),
                        _ => filtered.OrderByDescending(b => GetTimestampOrMin(b.UpdatedAt, b.CreatedAt))
                    };
                    return sorted.Skip(offsetValue).Take(limitValue).ToList();
                });

                return Ok(new { success = true, count = page?.Count ?? 0, data = page });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated books");
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(string id)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:id:{id}");
                var book = await _cache.GetOrSetAsync(cacheKey, () => _bookRepository.GetByIdAsync(id));

                if (book == null)
                {
                    return NotFound(new { success = false, message = $"Book with ID '{id}' not found" });
                }

                return Ok(new { success = true, data = book });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving book", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBookById(string id, [FromBody] JsonElement updates)
        {
            try
            {
                var updateDict = JsonElementToDictionary(updates);
                var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "title", "author", "authorId", "authorBio", "category", "price", "originalPrice",
                    "stockQuantity", "cover", "description", "isbn", "pages", "publisher", 
                    "publishYear", "language", "badge", "isAvailable", "slug", "searchKeywords",
                    "quantitySold", "badges", "discountRate", "images", "dimensions", 
                    "translator", "bookLayout", "manufacturer"
                };

                var filteredUpdates = updateDict
                    .Where(kvp => allowedFields.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (filteredUpdates.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No valid fields provided for update" });
                }

                filteredUpdates["updatedAt"] = Timestamp.GetCurrentTimestamp();
                var updated = await _bookRepository.UpdateFieldsAsync(id, filteredUpdates);

                if (!updated)
                {
                    return NotFound(new { success = false, message = $"Book with ID '{id}' not found" });
                }

                _cache.BumpVersion("books");
                return Ok(new { success = true, message = "Book updated successfully", data = await _bookRepository.GetByIdAsync(id) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book by ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error updating book", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBookById(string id)
        {
            try
            {
                var deleted = await _bookRepository.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(new { success = false, message = $"Book with ID '{id}' not found" });
                }

                _cache.BumpVersion("books");
                return Ok(new { success = true, message = "Book deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error deleting book", error = ex.Message });
            }
        }

        [HttpDelete("isbn/{isbn}")]
        public async Task<IActionResult> DeleteBook(string isbn)
        {
            try
            {
                var deleted = await _bookRepository.DeleteByIsbnAsync(isbn);
                if (!deleted)
                {
                    return NotFound(new { success = false, message = $"Book with ISBN '{isbn}' not found" });
                }

                _cache.BumpVersion("books");
                return Ok(new { success = true, message = "Book deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ISBN: {Isbn}", isbn);
                return StatusCode(500, new { success = false, message = "Error deleting book", error = ex.Message });
            }
        }

        [HttpPost("{id}/increment-views")]
        public async Task<IActionResult> IncrementViewCount(string id)
        {
            try
            {
                var success = await _bookRepository.IncrementViewCountAsync(id);
                if (!success) return NotFound(new { success = false, message = "Book not found" });
                
                _cache.BumpVersion("books");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing view count: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error incrementing view count", error = ex.Message });
            }
        }

        [HttpGet("author/{name}")]
        public async Task<IActionResult> GetBooksByAuthor(string name)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:author:{name}");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var all = await _bookRepository.GetAllAsync();
                    return all.Where(b => string.Equals(b.Author, name, StringComparison.OrdinalIgnoreCase)).ToList();
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by author: {Name}", name);
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetBooksByCategory(string category)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"books:category:{category}");
                var books = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var result = await _bookRepository.GetByCategoryAsync(category);
                    return result.ToList();
                });

                return Ok(new { success = true, count = books?.Count ?? 0, data = books });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by category: {Category}", category);
                return StatusCode(500, new { success = false, message = "Error retrieving books", error = ex.Message });
            }
        }


        private static List<string> ExtractBookIds(JsonElement body)
        {
            var ids = new List<string>();
            if (body.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in body.EnumerateArray()) if (item.ValueKind == JsonValueKind.String) ids.Add(item.GetString()!);
            }
            else if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("bookIds", out var el) && el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray()) if (item.ValueKind == JsonValueKind.String) ids.Add(item.GetString()!);
            }
            return ids;
        }

        private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object?>();
            if (element.ValueKind != JsonValueKind.Object) return result;
            foreach (var property in element.EnumerateObject())
            {
                var val = ConvertJsonElement(property.Value);
                if (val != null) result[property.Name] = val;
            }
            return result;
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                _ => null
            };
        }

        private static DateTime GetTimestampOrMin(Timestamp updatedAt, Timestamp createdAt)
        {
            if (!updatedAt.Equals(default(Timestamp))) return updatedAt.ToDateTime();
            if (!createdAt.Equals(default(Timestamp))) return createdAt.ToDateTime();
            return DateTime.MinValue;
        }
    }
}
