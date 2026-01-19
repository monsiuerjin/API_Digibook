using API_DigiBook.Models;

namespace API_DigiBook.Repositories
{
    public class CategoryRepository : FirestoreRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ILogger<CategoryRepository> logger) 
            : base("categories", logger)
        {
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            // Category uses name as document ID
            return await GetByIdAsync(name);
        }
    }
}
