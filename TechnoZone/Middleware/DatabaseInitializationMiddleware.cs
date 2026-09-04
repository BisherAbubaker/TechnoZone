using TechnoZone.Data;

namespace TechnoZone.Middleware
{
    public class DatabaseInitializationMiddleware
    {
        private readonly RequestDelegate _next;
        private static bool _initialized = false;
        private readonly ILogger<DatabaseInitializationMiddleware> _logger;

        public DatabaseInitializationMiddleware(RequestDelegate next, ILogger<DatabaseInitializationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            if (!_initialized)
            {
                try
                {
                    _logger.LogInformation("Starting database initialization...");
                    var db = new DatabaseConnection(configuration);
                    db.InitializeDatabase();
                    _initialized = true;
                    _logger.LogInformation("Database initialization completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Database initialization error: {ex.GetType().Name} - {ex.Message}");
                    _logger.LogError($"Stack trace: {ex.StackTrace}");

                    // Mark as initialized anyway to prevent continuous retry loops
                    _initialized = true;

                    // Log warning but don't crash
                    _logger.LogWarning("Application continuing without database. Some features may not work.");
                }
            }

            await _next(context);
        }
    }
}
