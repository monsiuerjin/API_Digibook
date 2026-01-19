using API_DigiBook.Models;

namespace API_DigiBook.Repositories
{
    public interface IBookRepository : IRepository<Book>
    {
        Task<IEnumerable<Book>> GetByAuthorAsync(string authorId);
        Task<IEnumerable<Book>> GetByCategoryAsync(string category);
        Task<IEnumerable<Book>> SearchByTitleAsync(string title);
        Task<IEnumerable<Book>> GetTopRatedAsync(int count = 10);
    }
}
