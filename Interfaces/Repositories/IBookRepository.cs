using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Repositories
{
    public interface IBookRepository : IRepository<Book>
    {
        Task<Book?> GetByIsbnAsync(string isbn);
        Task<Book?> GetBySlugAsync(string slug);
        Task<IEnumerable<Book>> GetByAuthorAsync(string authorId);
        Task<IEnumerable<Book>> GetByCategoryAsync(string category);
        Task<IEnumerable<Book>> SearchByTitleAsync(string title);
        Task<IEnumerable<Book>> GetTopRatedAsync(int count = 10);
        Task<bool> IncrementViewCountAsync(string bookId);
        Task<IEnumerable<Book>> GetByIdsAsync(IEnumerable<string> bookIds);
        Task<bool> UpdateByIsbnAsync(string isbn, Book book);
        Task<bool> DeleteByIsbnAsync(string isbn);
        Task<Book?> PatchByIsbnAsync(string isbn, BookPatchDto patchDto);
    }
}
