using API_DigiBook.Models;

namespace API_DigiBook.Repositories
{
    public interface IAuthorRepository : IRepository<Author>
    {
        Task<IEnumerable<Author>> SearchByNameAsync(string name);
    }
}
