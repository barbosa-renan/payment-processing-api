using PaymentProcessingAPI.Infrastructure;
using Serilog;

namespace PaymentProcessingAPI.Extensions;

public static class DatabaseExtensions
{
    public static WebApplication InitializeDatabase(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            try
            {
                dbContext.Database.EnsureCreated();
                Log.Information("Database initialization completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while initializing the database");
            }
        }

        return app;
    }
}