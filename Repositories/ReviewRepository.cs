using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace API_DigiBook.Repositories
{
    public class ReviewRepository : FirestoreRepository<Review>, IReviewRepository
    {
        public ReviewRepository(ILogger<ReviewRepository> logger) 
            : base("reviews", logger)
        {
        }

        public override async Task<string> AddAsync(Review entity, string? customId = null)
        {
            try 
            {
                if (string.IsNullOrEmpty(entity.BookId))
                {
                    throw new ArgumentException("BookId is required for adding a review");
                }

                var collectionRef = _db.Collection("books").Document(entity.BookId).Collection("reviews");

                if (!string.IsNullOrEmpty(customId))
                {
                    var docRef = collectionRef.Document(customId);
                    await docRef.SetAsync(entity);
                    return customId;
                }
                else
                {
                    var docRef = await collectionRef.AddAsync(entity);
                    return docRef.Id;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding review to book {BookId}", entity.BookId);
                throw;
            }
        }

        public async Task<IEnumerable<Review>> GetByBookIdAsync(string bookId)
        {
            try
            {
                // Correct path: books/{bookId}/reviews
                var query = _db.Collection("books").Document(bookId).Collection("reviews")
                    .OrderByDescending("createdAt");
                
                var snapshot = await query.GetSnapshotAsync();
                var reviews = new List<Review>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var review = document.ConvertTo<Review>();
                        review.Id = document.Id;
                        reviews.Add(review);
                    }
                }

                return reviews;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting reviews by book {BookId}", bookId);
                throw;
            }
        }

        public async Task<IEnumerable<Review>> GetByUserIdAsync(string userId)
        {
            try
            {
                // Collection Group Query for nested reviews
                var query = _db.CollectionGroup("reviews")
                    .WhereEqualTo("userId", userId)
                    .OrderByDescending("createdAt");
                
                var snapshot = await query.GetSnapshotAsync();
                var reviews = new List<Review>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var review = document.ConvertTo<Review>();
                        review.Id = document.Id;
                        reviews.Add(review);
                    }
                }

                return reviews;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting reviews by user {UserId}", userId);
                throw;
            }
        }

        public async Task<double> GetAverageRatingByBookIdAsync(string bookId)
        {
            try
            {
                var reviews = await GetByBookIdAsync(bookId);
                if (!reviews.Any())
                {
                    return 0;
                }

                return reviews.Average(r => r.Rating);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating average rating for book {BookId}", bookId);
                throw;
            }
        }
    }
}
