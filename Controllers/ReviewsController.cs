using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(IReviewRepository reviewRepository, ILogger<ReviewsController> logger)
        {
            _reviewRepository = reviewRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all reviews
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllReviews()
        {
            try
            {
                var reviews = await _reviewRepository.GetAllAsync();

                return Ok(new
                {
                    success = true,
                    count = reviews.Count(),
                    data = reviews
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving reviews",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get review by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReviewById(string id)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(id);

                if (review == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Review with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = review
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review by ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving review",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get reviews by book ID
        /// </summary>
        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetReviewsByBookId(string bookId)
        {
            try
            {
                var reviews = await _reviewRepository.GetByBookIdAsync(bookId);

                return Ok(new
                {
                    success = true,
                    count = reviews.Count(),
                    data = reviews
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews by book ID: {BookId}", bookId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving reviews",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get reviews by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetReviewsByUserId(string userId)
        {
            try
            {
                var reviews = await _reviewRepository.GetByUserIdAsync(userId);

                return Ok(new
                {
                    success = true,
                    count = reviews.Count(),
                    data = reviews
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews by user ID: {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving reviews",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get average rating for a book
        /// </summary>
        [HttpGet("book/{bookId}/average-rating")]
        public async Task<IActionResult> GetAverageRating(string bookId)
        {
            try
            {
                var avgRating = await _reviewRepository.GetAverageRatingByBookIdAsync(bookId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        bookId = bookId,
                        averageRating = avgRating
                    },
                    averageRating = avgRating
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting average rating for book: {BookId}", bookId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error calculating average rating",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new review
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] Review review)
        {
            try
            {
                review.CreatedAt = Timestamp.GetCurrentTimestamp();

                var reviewId = await _reviewRepository.AddAsync(review, review.Id);
                review.Id = reviewId;

                return CreatedAtAction(nameof(GetReviewById), new { id = review.Id }, new
                {
                    success = true,
                    message = "Review created successfully",
                    data = review
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating review",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing review
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(string id, [FromBody] Review review)
        {
            try
            {
                review.Id = id;

                var updated = await _reviewRepository.UpdateAsync(id, review);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Review with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Review updated successfully",
                    data = review
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating review",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a review
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(string id)
        {
            try
            {
                var deleted = await _reviewRepository.DeleteAsync(id);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Review with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Review deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting review",
                    error = ex.Message
                });
            }
        }
    }
}
