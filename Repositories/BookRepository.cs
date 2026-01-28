using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using Google.Cloud.Firestore;

namespace API_DigiBook.Repositories
{
    public class BookRepository : FirestoreRepository<Book>, IBookRepository
    {
        public BookRepository(ILogger<BookRepository> logger) 
            : base("books", logger)
        {
        }

        public async Task<Book?> GetByIsbnAsync(string isbn)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("isbn", isbn)
                    .Limit(1);
                
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    return null;
                }

                var document = snapshot.Documents[0];
                if (document.Exists)
                {
                    var book = document.ConvertTo<Book>();
                    book.Id = document.Id;
                    return book;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting book by ISBN {Isbn}", isbn);
                throw;
            }
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

        public async Task<Book?> GetBySlugAsync(string slug)
        {
            try
            {
                var query = _db.Collection(_collectionName)
                    .WhereEqualTo("slug", slug)
                    .Limit(1);
                
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    return null;
                }

                var document = snapshot.Documents[0];
                if (document.Exists)
                {
                    var book = document.ConvertTo<Book>();
                    book.Id = document.Id;
                    return book;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting book by slug {Slug}", slug);
                throw;
            }
        }

        public async Task<bool> IncrementViewCountAsync(string bookId)
        {
            try
            {
                var docRef = _db.Collection(_collectionName).Document(bookId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return false;
                }

                await docRef.UpdateAsync("viewCount", FieldValue.Increment(1));
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error incrementing view count for book {BookId}", bookId);
                throw;
            }
        }

        public async Task<IEnumerable<Book>> GetByIdsAsync(IEnumerable<string> bookIds)
        {
            try
            {
                var books = new List<Book>();
                
                if (!bookIds.Any())
                {
                    return books;
                }

                // Firestore has a limit of 10 items for "in" queries
                // So we need to batch the requests
                var bookIdBatches = bookIds
                    .Select((id, index) => new { id, index })
                    .GroupBy(x => x.index / 10)
                    .Select(g => g.Select(x => x.id).ToList());

                foreach (var batch in bookIdBatches)
                {
                    var query = _db.Collection(_collectionName)
                        .WhereIn(FieldPath.DocumentId, batch);
                    
                    var snapshot = await query.GetSnapshotAsync();

                    foreach (var document in snapshot.Documents)
                    {
                        if (document.Exists)
                        {
                            var book = document.ConvertTo<Book>();
                            book.Id = document.Id;
                            books.Add(book);
                        }
                    }
                }

                return books;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting books by IDs");
                throw;
            }
        }
    }
}
