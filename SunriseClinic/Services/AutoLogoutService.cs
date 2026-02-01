using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SunriseClinic.Services
{
    public class AutoLogoutService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoLogoutService> _logger;

        public AutoLogoutService(IServiceProvider serviceProvider, ILogger<AutoLogoutService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto Logout Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var connectionString = scope.ServiceProvider
                            .GetRequiredService<IConfiguration>()
                            .GetConnectionString("DefaultConnection");

                        await CleanupExpiredSessions(connectionString);
                    }

                    // প্রতি ৫ মিনিট পর পর চেক করুন
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Auto Logout Service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task CleanupExpiredSessions(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // 1. RememberMe না থাকা users যাদের 24 ঘণ্টা হয়ে গেছে
                var query1 = @"
                    UPDATE Users 
                    SET RememberMe = 0, 
                        RememberMeExpiry = NULL,
                        LastActivity = NULL
                    WHERE RememberMe = 0 
                    AND LastActivity < DATEADD(HOUR, -24, GETDATE())";

                using (var cmd = new SqlCommand(query1, connection))
                {
                    var affected1 = await cmd.ExecuteNonQueryAsync();
                    if (affected1 > 0)
                    {
                        _logger.LogInformation($"Auto-logout {affected1} non-remembered users (24h expired)");
                    }
                }

                // 2. RememberMe থাকা users যাদের 30 দিন হয়ে গেছে
                var query2 = @"
                    UPDATE Users 
                    SET RememberMe = 0, 
                        RememberMeExpiry = NULL,
                        LastActivity = NULL
                    WHERE RememberMe = 1 
                    AND RememberMeExpiry < GETDATE()";

                using (var cmd = new SqlCommand(query2, connection))
                {
                    var affected2 = await cmd.ExecuteNonQueryAsync();
                    if (affected2 > 0)
                    {
                        _logger.LogInformation($"Auto-logout {affected2} remembered users (30 days expired)");
                    }
                }
            }
        }
    }
}