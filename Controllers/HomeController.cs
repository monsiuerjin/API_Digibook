using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Services;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<HomeController> _logger;
        private readonly ICacheService _cache;

        public HomeController(
            IBookRepository bookRepository,
            ICategoryRepository categoryRepository,
            ILogger<HomeController> logger,
            ICacheService cache)
        {
            _bookRepository = bookRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetHome(
            [FromQuery] int featured = 8,
            [FromQuery] int topRated = 8)
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey($"home:payload:{featured}:{topRated}");
                var cached = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var allBooks = await _bookRepository.GetAllAsync();
                    var featuredBooks = allBooks
                        .OrderByDescending(b => GetTimestampOrMin(b.UpdatedAt, b.CreatedAt))
                        .Take(Math.Max(1, featured))
                        .ToList();

                    var topRatedBooks = (await _bookRepository.GetTopRatedAsync(Math.Max(1, topRated))).ToList();
                    var categories = (await _categoryRepository.GetAllAsync()).ToList();

                    return new
                    {
                        featured = featuredBooks,
                        topRated = topRatedBooks,
                        categories = categories
                    };
                });

                return Ok(new
                {
                    success = true,
                    data = cached
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building home payload");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving home data",
                    error = ex.Message
                });
            }
        }

        private static DateTime GetTimestampOrMin(Timestamp updatedAt, Timestamp createdAt)
        {
            if (!updatedAt.Equals(default(Timestamp)))
            {
                return updatedAt.ToDateTime();
            }

            if (!createdAt.Equals(default(Timestamp)))
            {
                return createdAt.ToDateTime();
            }

            return DateTime.MinValue;
        }
    }
}
