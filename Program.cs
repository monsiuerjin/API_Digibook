using DotNetEnv;
using API_DigiBook.Services;
using API_DigiBook.Notifications;
using API_DigiBook.Notifications.Channels;
using API_DigiBook.Notifications.Configuration;
using API_DigiBook.Notifications.Contracts;
using API_DigiBook.Notifications.Observers;
using API_DigiBook.Interfaces.Services;
using API_DigiBook.Models;
using API_DigiBook.Services.Chat;
using API_DigiBook.Converters;
using System.Text.Json;

namespace API_DigiBook
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string frontendCorsPolicy = "FrontendCorsPolicy";

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
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new FirestoreTimestampConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
            
            // Register Repositories
            builder.Services.AddScoped<API_DigiBook.Interfaces.Repositories.IBookRepository, API_DigiBook.Repositories.BookRepository>();
            builder.Services.AddScoped<API_DigiBook.Interfaces.Repositories.ICategoryRepository, API_DigiBook.Repositories.CategoryRepository>();
            builder.Services.AddScoped<API_DigiBook.Interfaces.Repositories.IAuthorRepository, API_DigiBook.Repositories.AuthorRepository>();
            builder.Services.AddScoped<API_DigiBook.Interfaces.Repositories.IUserRepository, API_DigiBook.Repositories.UserRepository>();
            builder.Services.AddScoped<API_DigiBook.Interfaces.Repositories.IOrderRepository, API_DigiBook.Repositories.OrderRepository>();
            builder.Services.AddScoped<API_DigiBook.Interfaces.Repositories.IReviewRepository, API_DigiBook.Repositories.ReviewRepository>();
            builder.Services.AddScoped<API_DigiBook.Interfaces.Repositories.ICouponRepository, API_DigiBook.Repositories.CouponRepository>();
            builder.Services.AddScoped<API_DigiBook.Interfaces.Repositories.INotificationLogRepository, API_DigiBook.Repositories.NotificationLogRepository>();
            
            // Register Payment services
            builder.Services.AddHttpClient<API_DigiBook.Services.Payment.PayOSService>();
            builder.Services.AddScoped<API_DigiBook.Factories.PaymentServiceFactory>();

            // Register Notification services (Observer Pattern)
            builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notification"));
            builder.Services.Configure<GmailApiOptions>(builder.Configuration.GetSection("GmailApi"));
            builder.Services.AddHttpClient<TelegramNotificationChannel>();
            builder.Services.AddHttpClient<GmailApiEmailNotificationChannel>((sp, client) =>
            {
                var notificationOptions = sp
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<NotificationOptions>>()
                    .Value;

                client.Timeout = TimeSpan.FromMilliseconds(Math.Max(1000, notificationOptions.Email.TimeoutMilliseconds));
            });
            builder.Services.AddHttpClient<ResendEmailNotificationChannel>((sp, client) =>
            {
                var notificationOptions = sp
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<NotificationOptions>>()
                    .Value;

                var baseUrl = notificationOptions.Email.Resend.BaseUrl;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    baseUrl = "https://api.resend.com";
                }

                client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
                client.Timeout = TimeSpan.FromMilliseconds(Math.Max(1000, notificationOptions.Email.TimeoutMilliseconds));
            });
            builder.Services.AddScoped<SmtpEmailNotificationChannel>();
            builder.Services.AddScoped<FallbackEmailNotificationChannel>();
            builder.Services.AddScoped<IEmailNotificationChannel>(sp =>
                sp.GetRequiredService<FallbackEmailNotificationChannel>());
            builder.Services.AddScoped<INotificationObserver, EmailNotificationObserver>();
            builder.Services.AddScoped<INotificationObserver, TelegramNotificationObserver>();
            builder.Services.AddScoped<INotificationPublisher, NotificationPublisher>();
            
            // Register Facade Pattern services
            builder.Services.AddScoped<API_DigiBook.Interfaces.Services.IOrderCheckoutFacade, API_DigiBook.Services.Orders.OrderCheckoutFacade>();
            
            // Register Command Pattern services
            builder.Services.AddScoped<API_DigiBook.Commands.CommandInvoker>();

            // Register Chatbot services
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.Configure<ChatbotOptions>(builder.Configuration.GetSection("Chatbot"));
            builder.Services.AddScoped<IChatRecommendationService, ChatRecommendationService>();
            builder.Services.AddHttpClient<IGeminiClient, GeminiClient>();
            
            // Add CORS policy
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(frontendCorsPolicy, corsBuilder =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        corsBuilder.WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                        return;
                    }

                    // Fallback for quick troubleshooting when explicit origins are not configured.
                    corsBuilder.SetIsOriginAllowed(_ => true)
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
            app.UseCors(frontendCorsPolicy);

            app.UseAuthorization();

            app.MapControllers();
            
            // Health check endpoint for cron-job.org to keep Render alive
            app.MapGet("/ping", () => Results.Ok("pong"));

            Console.WriteLine("🚀 API_DigiBook is running...");

            var externalUrl = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL") 
                             ?? Environment.GetEnvironmentVariable("API_URL");
            
            if (!string.IsNullOrEmpty(externalUrl))
            {
                Console.WriteLine($"🌐 Public API URL: {externalUrl}");
                Console.WriteLine($"📖 Swagger UI: {externalUrl.TrimEnd('/')}/swagger");
            }
            else
            {
                Console.WriteLine($"📖 Swagger UI: {app.Urls.FirstOrDefault() ?? "http://localhost:5197"}/swagger");
            }
            
            // Auto-open browser to Swagger (works with dotnet run)
            if (app.Environment.IsDevelopment())
            {
                var url = app.Urls.FirstOrDefault() ?? "http://localhost:5197";
                var swaggerUrl = $"{url}/swagger";
                
                try
                {
                    // Open browser automatically
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = swaggerUrl,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    Console.WriteLine($"✅ Browser opened: {swaggerUrl}");
                }
                catch (Exception)
                {
                    Console.WriteLine($"💡 Please open: {swaggerUrl}");
                }
            }

            app.Run();
        }
    }
}
