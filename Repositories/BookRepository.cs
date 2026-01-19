using API_DigiBook.Models;
using Google.Cloud.Firestore;

namespace API_DigiBook.Repositories
{
    public class BookRepository : FirestoreRepository<Book>, IBookRepository
    {
        public BookRepository(ILogger<BookRepository> logger) 
            : base("books", logger)
        {
        }

        public async Task<IEnumerable<Book>> GetByAuthorAsync(string authorId)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("authorId", authorId);
                
                var snapshot = await query.GetSnapshotAsync();
                var books = new List<Book>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var book = document.ConvertTo<Book>();
                        book.Id = document.Id;
                        books.Add(book);
                    }
                }

                return books;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting books by author {AuthorId}", authorId);
                throw;
            }
        }

        public async Task<IEnumerable<Book>> GetByCategoryAsync(string category)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("category", category);
                
                var snapshot = await query.GetSnapshotAsync();
                var books = new List<Book>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var book = document.ConvertTo<Book>();
                        book.Id = document.Id;
                        books.Add(book);
                    }
                }

                return books;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting books by category {Category}", category);
                throw;
            }
        }

        public async Task<IEnumerable<Book>> SearchByTitleAsync(string title)
        {
            try
            {
                // Firestore doesn't support full-text search natively
                // This is a simple contains check on client side
                var allBooks = await GetAllAsync();
                return allBooks.Where(b => 
                    b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching books by title {Title}", title);
                throw;
            }
        }

        public async Task<IEnumerable<Book>> GetTopRatedAsync(int count = 10)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .OrderByDescending("rating")
                    .Limit(count);
                
                var snapshot = await query.GetSnapshotAsync();
                var books = new List<Book>();

                foreach (var document in snapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var book = document.ConvertTo<Book>();
                        book.Id = document.Id;
                        books.Add(book);
                    }
                }

                return books;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting top rated books");
                throw;
            }
        }
    }
}
