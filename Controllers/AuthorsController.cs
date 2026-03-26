using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using System.Text.Json;
using API_DigiBook.Services;
using Microsoft.Extensions.Caching.Memory;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorsController : ControllerBase
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly ILogger<AuthorsController> _logger;
        private readonly IMemoryCache _cache;

        private const int CacheMinutes = 2;
        private const string AuthorsVersionKey = "cache:authors:version";

        public AuthorsController(IAuthorRepository authorRepository, ILogger<AuthorsController> logger, IMemoryCache cache)
        {
            _authorRepository = authorRepository;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Get all authors
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllAuthors()
        {
            try
            {
                var version = GetCacheVersion(AuthorsVersionKey);
                var cacheKey = $"cache:authors:all:{version}";
                if (!_cache.TryGetValue(cacheKey, out List<Author>? authors))
                {
                    authors = (await _authorRepository.GetAllAsync()).ToList();
                    _cache.Set(cacheKey, authors, TimeSpan.FromMinutes(CacheMinutes));
                    CacheReadMonitor.Record("authors:all", _logger);
                }

                return Ok(new
                {
                    success = true,
                    count = authors.Count,
                    data = authors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authors");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving authors",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get author by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuthorById(string id)
        {
            try
            {
                var author = await _authorRepository.GetByIdAsync(id);

                if (author == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Author with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = author
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting author by ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving author",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Search authors by name
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchAuthors([FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Name parameter is required"
                    });
                }

                var authors = await _authorRepository.SearchByNameAsync(name);

                return Ok(new
                {
                    success = true,
                    count = authors.Count(),
                    data = authors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching authors by name: {Name}", name);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error searching authors",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get author by name (case-insensitive)
        /// </summary>
        [HttpGet("by-name/{name}")]
        public async Task<IActionResult> GetAuthorByName(string name)
        {
            try
            {
                var authors = await _authorRepository.GetAllAsync();
                var author = authors.FirstOrDefault(a =>
                    string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

                if (author == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Author '{name}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = author
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting author by name: {Name}", name);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving author",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new author
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAuthor([FromBody] Author author)
        {
            try
            {
                author.CreatedAt = Timestamp.GetCurrentTimestamp();

                var authorId = await _authorRepository.AddAsync(author, author.Id);
                author.Id = authorId;
                BumpCacheVersion(AuthorsVersionKey);

                return CreatedAtAction(nameof(GetAuthorById), new { id = author.Id }, new
                {
                    success = true,
                    message = "Author created successfully",
                    data = author
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating author");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating author",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing author
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuthor(string id, [FromBody] Author author)
        {
            try
            {
                author.Id = id;

                var updated = await _authorRepository.UpdateAsync(id, author);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Author with ID '{id}' not found"
                    });
                }

                BumpCacheVersion(AuthorsVersionKey);

                return Ok(new
                {
                    success = true,
                    message = "Author updated successfully",
                    data = author
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating author with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating author",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete an author
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor(string id)
        {
            try
            {
                var deleted = await _authorRepository.DeleteAsync(id);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Author with ID '{id}' not found"
                    });
                }

                BumpCacheVersion(AuthorsVersionKey);

                return Ok(new
                {
                    success = true,
                    message = "Author deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting author with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting author",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Bulk delete authors
        /// </summary>
        [HttpPost("bulk-delete")]
        public async Task<IActionResult> BulkDelete([FromBody] JsonElement body)
        {
            try
            {
                var ids = ExtractIds(body);
                if (ids.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Author IDs are required"
                    });
                }

                int deletedCount = 0;
                foreach (var id in ids)
                {
                    if (await _authorRepository.DeleteAsync(id))
                    {
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    BumpCacheVersion(AuthorsVersionKey);
                }

                return Ok(new
                {
                    success = true,
                    message = $"Deleted {deletedCount} authors",
                    data = new { deletedCount }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting authors");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting authors",
                    error = ex.Message
                });
            }
        }

        private static List<string> ExtractIds(JsonElement body)
        {
            var ids = new List<string>();
            if (body.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in body.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        ids.Add(item.GetString()!);
                    }
                }
            }
            else if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("ids", out var idsElement))
            {
                if (idsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in idsElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        {
                            ids.Add(item.GetString()!);
                        }
                    }
                }
            }

            return ids;
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
