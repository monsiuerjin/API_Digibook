using API_DigiBook.Models;

namespace API_DigiBook.Interfaces.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category?> GetByNameAsync(string name);
    }
}
