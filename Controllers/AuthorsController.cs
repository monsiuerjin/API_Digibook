using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Repositories;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorsController : ControllerBase
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly ILogger<AuthorsController> _logger;

        public AuthorsController(IAuthorRepository authorRepository, ILogger<AuthorsController> logger)
        {
            _authorRepository = authorRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all authors
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllAuthors()
        {
            try
            {
                var authors = await _authorRepository.GetAllAsync();

                return Ok(new
                {
                    success = true,
                    count = authors.Count(),
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
    }
}
