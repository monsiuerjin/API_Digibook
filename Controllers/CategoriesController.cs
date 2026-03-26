using Microsoft.AspNetCore.Mvc;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using System.Text.Json;
using API_DigiBook.Services;
using Microsoft.Extensions.Caching.Memory;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoriesController> _logger;
        private readonly IMemoryCache _cache;

        private const int CacheMinutes = 2;
        private const string CategoriesVersionKey = "cache:categories:version";

        public CategoriesController(ICategoryRepository categoryRepository, ILogger<CategoriesController> logger, IMemoryCache cache)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var version = GetCacheVersion(CategoriesVersionKey);
                var cacheKey = $"cache:categories:all:{version}";
                if (!_cache.TryGetValue(cacheKey, out List<Category>? categories))
                {
                    categories = (await _categoryRepository.GetAllAsync()).ToList();
                    _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(CacheMinutes));
                    CacheReadMonitor.Record("categories:all", _logger);
                }
                
                return Ok(new
                {
                    success = true,
                    count = categories.Count,
                    data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving categories",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get category by name (Document ID)
        /// </summary>
        [HttpGet("{name}")]
        public async Task<IActionResult> GetCategoryByName(string name)
        {
            try
            {
                var category = await _categoryRepository.GetByNameAsync(name);

                if (category == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Category '{name}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category: {Name}", name);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving category",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            try
            {
                if (string.IsNullOrEmpty(category.Name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Category name is required"
                    });
                }

                // Check if category already exists
                var exists = await _categoryRepository.ExistsAsync(category.Name);
                if (exists)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = $"Category '{category.Name}' already exists"
                    });
                }

                // Use name as document ID
                await _categoryRepository.AddAsync(category, category.Name);
                BumpCacheVersion(CategoriesVersionKey);

                return CreatedAtAction(nameof(GetCategoryByName), new { name = category.Name }, new
                {
                    success = true,
                    message = "Category created successfully",
                    data = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating category",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing category
        /// </summary>
        [HttpPut("{name}")]
        public async Task<IActionResult> UpdateCategory(string name, [FromBody] Category category)
        {
            try
            {
                var updated = await _categoryRepository.UpdateAsync(name, category);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Category '{name}' not found"
                    });
                }

                BumpCacheVersion(CategoriesVersionKey);

                return Ok(new
                {
                    success = true,
                    message = "Category updated successfully",
                    data = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {Name}", name);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating category",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a category
        /// </summary>
        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteCategory(string name)
        {
            try
            {
                var deleted = await _categoryRepository.DeleteAsync(name);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Category '{name}' not found"
                    });
                }

                BumpCacheVersion(CategoriesVersionKey);

                return Ok(new
                {
                    success = true,
                    message = "Category deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {Name}", name);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting category",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Bulk delete categories
        /// </summary>
        [HttpPost("bulk-delete")]
        public async Task<IActionResult> BulkDelete([FromBody] JsonElement body)
        {
            try
            {
                var names = ExtractNames(body);
                if (names.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Category names are required"
                    });
                }

                int deletedCount = 0;
                foreach (var name in names)
                {
                    if (await _categoryRepository.DeleteAsync(name))
                    {
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    BumpCacheVersion(CategoriesVersionKey);
                }

                return Ok(new
                {
                    success = true,
                    message = $"Deleted {deletedCount} categories",
                    data = new { deletedCount }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting categories");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting categories",
                    error = ex.Message
                });
            }
        }

        private static List<string> ExtractNames(JsonElement body)
        {
            var names = new List<string>();
            if (body.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in body.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        names.Add(item.GetString()!);
                    }
                }
            }
            else if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("names", out var namesElement))
            {
                if (namesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in namesElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        {
                            names.Add(item.GetString()!);
                        }
                    }
                }
            }

            return names;
        }

        private string GetCacheVersion(string key)
        {
            if (!_cache.TryGetValue(key, out string? version) || string.IsNullOrWhiteSpace(version))
            {
                version = Guid.NewGuid().ToString("N");
                _cache.Set(key, version, TimeSpan.FromMinutes(CacheMinutes));
            }

            return version;
        }

        private void BumpCacheVersion(string key)
        {
            _cache.Set(key, Guid.NewGuid().ToString("N"), TimeSpan.FromMinutes(CacheMinutes));
        }
    }
}
