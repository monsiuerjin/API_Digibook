using DotNetEnv;
using API_DigiBook.Services;

namespace API_DigiBook
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Load environment variables from .env file
            try
            {
                Env.Load();
                Console.WriteLine("✓ Environment variables loaded from .env file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning: Could not load .env file: {ex.Message}");
                Console.WriteLine("Please create a .env file based on .env.template");
            }

            var builder = WebApplication.CreateBuilder(args);

            // Initialize Firebase
            try
            {
                FirebaseService.InitializeFirebase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing Firebase: {ex.Message}");
                throw;
            }

            // Add services to the container
            builder.Services.AddControllers();
            
            // Register Repositories
            builder.Services.AddScoped<API_DigiBook.Repositories.IBookRepository, API_DigiBook.Repositories.BookRepository>();
            builder.Services.AddScoped<API_DigiBook.Repositories.ICategoryRepository, API_DigiBook.Repositories.CategoryRepository>();
            builder.Services.AddScoped<API_DigiBook.Repositories.IAuthorRepository, API_DigiBook.Repositories.AuthorRepository>();
            builder.Services.AddScoped<API_DigiBook.Repositories.IUserRepository, API_DigiBook.Repositories.UserRepository>();
            builder.Services.AddScoped<API_DigiBook.Repositories.IOrderRepository, API_DigiBook.Repositories.OrderRepository>();
            builder.Services.AddScoped<API_DigiBook.Repositories.IReviewRepository, API_DigiBook.Repositories.ReviewRepository>();
            builder.Services.AddScoped<API_DigiBook.Repositories.ICouponRepository, API_DigiBook.Repositories.CouponRepository>();
            
            // Add CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Use CORS
            app.UseCors("AllowAll");

            app.UseAuthorization();

            app.MapControllers();

            Console.WriteLine("🚀 API_DigiBook is running...");
            app.Run();
        }
    }
}
