using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Singleton;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILogger<BooksController> _logger;
        private readonly LoggerService _systemLogger;

        public BooksController(IBookRepository bookRepository, ILogger<BooksController> logger)
        {
            _bookRepository = bookRepository;
            _logger = logger;
            _systemLogger = LoggerService.Instance; // Get singleton instance
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
        /// Get book by ISBN
        /// </summary>
        [HttpGet("isbn/{isbn}")]
        public async Task<IActionResult> GetBookByIsbn(string isbn)
        {
            try
            {
                // Log request
                await _systemLogger.LogInfoAsync(
                    "GET_BOOK_BY_ISBN",
                    $"Request to get book with ISBN: {isbn}",
                    "Anonymous"
                );

                var book = await _bookRepository.GetByIsbnAsync(isbn);

                if (book == null)
                {
                    // Log not found
                    await _systemLogger.LogWarningAsync(
                        "BOOK_NOT_FOUND",
                        $"Book with ISBN '{isbn}' not found",
                        "Anonymous"
                    );

                    return NotFound(new
                    {
                        success = false,
                        message = $"Book with ISBN '{isbn}' not found"
                    });
                }

                // Log success
                await _systemLogger.LogSuccessAsync(
                    "GET_BOOK_BY_ISBN",
                    $"Successfully retrieved book: {book.Title} (ISBN: {isbn})",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = book
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by ISBN: {Isbn}", isbn);
                
                // Log error to system
                await _systemLogger.LogErrorAsync(
                    "GET_BOOK_BY_ISBN",
                    $"Error getting book with ISBN '{isbn}': {ex.Message}",
                    "Anonymous"
                );

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

                // Log success
                await _systemLogger.LogSuccessAsync(
                    "CREATE_BOOK",
                    $"Book created: {book.Title} (ISBN: {book.Isbn}, ID: {bookId})",
                    "Admin"
                );

                return CreatedAtAction(nameof(GetBookByIsbn), new { isbn = book.Isbn }, new
                {
                    success = true,
                    message = "Book created successfully",
                    data = book
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                
                // Log error
                await _systemLogger.LogErrorAsync(
                    "CREATE_BOOK",
                    $"Failed to create book: {ex.Message}",
                    "Admin"
                );

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating book",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing book by ISBN
        /// </summary>
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
                    await _systemLogger.LogWarningAsync(
                        "UPDATE_BOOK",
                        $"Attempted to update non-existent book with ISBN: {isbn}",
                        "Admin"
                    );

                    return NotFound(new
                    {
                        success = false,
                        message = $"Book with ISBN '{isbn}' not found"
                    });
                }

                await _systemLogger.LogSuccessAsync(
                    "UPDATE_BOOK",
                    $"Book updated: {book.Title} (ISBN: {isbn})",
                    "Admin"
                );

                return Ok(new
                {
                    success = true,
                    message = "Book updated successfully",
                    data = book
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book with ISBN: {Isbn}", isbn);
                
                await _systemLogger.LogErrorAsync(
                    "UPDATE_BOOK",
                    $"Error updating book with ISBN '{isbn}': {ex.Message}",
                    "Admin"
                );

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating book",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a book by ISBN
        /// </summary>
        [HttpDelete("isbn/{isbn}")]
        public async Task<IActionResult> DeleteBook(string isbn)
        {
            try
            {
                // Get book first to retrieve title for logging
                var book = await _bookRepository.GetByIsbnAsync(isbn);
                
                var deleted = await _bookRepository.DeleteByIsbnAsync(isbn);

                if (!deleted)
                {
                    await _systemLogger.LogWarningAsync(
                        "DELETE_BOOK",
                        $"Attempted to delete non-existent book with ISBN: {isbn}",
                        "Admin"
                    );

                    return NotFound(new
                    {
                        success = false,
                        message = $"Book with ISBN '{isbn}' not found"
                    });
                }

                // Log success
                await _systemLogger.LogSuccessAsync(
                    "DELETE_BOOK",
                    $"Book deleted: {book?.Title ?? "Unknown"} (ISBN: {isbn})",
                    "Admin"
                );

                return Ok(new
                {
                    success = true,
                    message = "Book deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ISBN: {Isbn}", isbn);
                
                await _systemLogger.LogErrorAsync(
                    "DELETE_BOOK",
                    $"Error deleting book with ISBN '{isbn}': {ex.Message}",
                    "Admin"
                );

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

        /// <summary>
        /// Get book by slug (SEO-friendly URL)
        /// </summary>
        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBookBySlug(string slug)
        {
            try
            {
                var book = await _bookRepository.GetBySlugAsync(slug);

                if (book == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Book with slug '{slug}' not found"
                    });
                }

                // Increment view count when book is viewed
                await _bookRepository.IncrementViewCountAsync(book.Id);

                return Ok(new
                {
                    success = true,
                    data = book
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book by slug: {Slug}", slug);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving book",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get multiple books by IDs (for wishlist)
        /// </summary>
        [HttpPost("by-ids")]
        public async Task<IActionResult> GetBooksByIds([FromBody] List<string> bookIds)
        {
            try
            {
                if (bookIds == null || !bookIds.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Book IDs are required"
                    });
                }

                var books = await _bookRepository.GetByIdsAsync(bookIds);

                return Ok(new
                {
                    success = true,
                    count = books.Count(),
                    data = books
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books by IDs");
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
