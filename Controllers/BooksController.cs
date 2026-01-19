using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Repositories;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILogger<BooksController> _logger;

        public BooksController(IBookRepository bookRepository, ILogger<BooksController> logger)
        {
            _bookRepository = bookRepository;
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint to check Firebase connection
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var count = await _bookRepository.CountAsync();
                
                return Ok(new
                {
                    success = true,
                    message = "Firebase connection successful!",
                    booksCount = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Firebase connection");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Firebase connection failed",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all books
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllBooks()
        {
            try
            {
                var books = await _bookRepository.GetAllAsync();

                return Ok(new
                {
                    success = true,
                    count = books.Count(),
                    data = books
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving books",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get book by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(string id)
        {
            try
            {
                var book = await _bookRepository.GetByIdAsync(id);

                if (book == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Book with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = book
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving book",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new book
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateBook([FromBody] Book book)
        {
            try
            {
                book.CreatedAt = Timestamp.GetCurrentTimestamp();
                book.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var bookId = await _bookRepository.AddAsync(book, book.Id);
                book.Id = bookId;

                return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, new
                {
                    success = true,
                    message = "Book created successfully",
                    data = book
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating book",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing book
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(string id, [FromBody] Book book)
        {
            try
            {
                book.Id = id;
                book.UpdatedAt = Timestamp.GetCurrentTimestamp();
                
                var updated = await _bookRepository.UpdateAsync(id, book);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Book with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Book updated successfully",
                    data = book
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating book",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a book
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(string id)
        {
            try
            {
                var deleted = await _bookRepository.DeleteAsync(id);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Book with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Book deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting book",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get books by author
        /// </summary>
        [HttpGet("author/{authorId}")]
        public async Task<IActionResult> GetBooksByAuthor(string authorId)
        {
            try
            {
                var books = await _bookRepository.GetByAuthorAsync(authorId);

                return Ok(new
                {
                    success = true,
                    count = books.Count(),
                    data = books
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by author: {AuthorId}", authorId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving books",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get books by category
        /// </summary>
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetBooksByCategory(string category)
        {
            try
            {
                var books = await _bookRepository.GetByCategoryAsync(category);

                return Ok(new
                {
                    success = true,
                    count = books.Count(),
                    data = books
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by category: {Category}", category);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving books",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Search books by title
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Title parameter is required"
                    });
                }

                var books = await _bookRepository.SearchByTitleAsync(title);

                return Ok(new
                {
                    success = true,
                    count = books.Count(),
                    data = books
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching books by title: {Title}", title);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error searching books",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get top rated books
        /// </summary>
        [HttpGet("top-rated")]
        public async Task<IActionResult> GetTopRatedBooks([FromQuery] int count = 10)
        {
            try
            {
                var books = await _bookRepository.GetTopRatedAsync(count);

                return Ok(new
                {
                    success = true,
                    count = books.Count(),
                    data = books
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top rated books");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving books",
                    error = ex.Message
                });
            }
        }
    }
}
