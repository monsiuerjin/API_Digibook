using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;

namespace API_DigiBook.Repositories
{
    public class AuthorRepository : FirestoreRepository<Author>, IAuthorRepository
    {
        public AuthorRepository(ILogger<AuthorRepository> logger) 
            : base("authors", logger)
        {
        }

        public async Task<IEnumerable<Author>> SearchByNameAsync(string name)
        {
            try
            {
                var allAuthors = await GetAllAsync();
                return allAuthors.Where(a => 
                    a.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching authors by name {Name}", name);
                throw;
            }
        }
    }
}
