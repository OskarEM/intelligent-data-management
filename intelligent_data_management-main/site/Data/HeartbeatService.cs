using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;

// ... other using directives



namespace Site.Data
{
  

    public class HeartbeatService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly ILogger<HeartbeatService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public HeartbeatService(ILogger<HeartbeatService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Heartbeat Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromMinutes(5)); 

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _ = Task.Run(async () =>
            {
                await CheckDatabaseAvailabilityAsync(_serviceProvider);
                await CheckMongoDbAvailabilityAsync(_serviceProvider);
                await CheckRedisAvailabilityAsync(_serviceProvider);
                
            });
        }



        
        private async Task CheckDatabaseAvailabilityAsync(IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    // Using the asynchronous method to check the database connection
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        _logger.LogInformation("Postgres is available.");
                    }
                    else
                    {
                        _logger.LogWarning("Postgres is unavailable.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Postgres check failed: {ex.Message}");
                   
                }
            }
        }


       private async Task CheckRedisAvailabilityAsync(IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                var connectionMultiplexer = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                try
                {
                    // Perform a simple PING to check if Redis is available
                    var db = connectionMultiplexer.GetDatabase();
                    var pingResult = await db.PingAsync();
                    _logger.LogInformation($"Redis is available. Ping took: {pingResult.TotalMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Redis check failed: {ex.Message}");
                    
                }
            }
        }




        private async Task CheckMongoDbAvailabilityAsync(IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                var mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
                try
                {
                    var database = mongoClient.GetDatabase("MyAppDatabase");
                    // Await the asynchronous operation and then call ToListAsync on the result
                    var collectionsCursor = await database.ListCollectionNamesAsync();
                    var collections = await collectionsCursor.ToListAsync();
                    _logger.LogInformation($"MongoDB is available. Collections count: {collections.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"MongoDB check failed: {ex.Message}");
                    
                }
            }
        }



        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Heartbeat Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }

}