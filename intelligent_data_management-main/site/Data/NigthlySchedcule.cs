using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using Site.Data;
using Site.Models;

namespace Site.Data
{
    public class NightlyDataSyncService : IHostedService
    {
        private readonly ILogger<NightlyDataSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Task _executingTask;
        private CancellationTokenSource _stoppingCts;

        public NightlyDataSyncService(
            ILogger<NightlyDataSyncService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = ExecuteAsync(_stoppingCts.Token);
            return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Nightly Data Sync Service starting initial delay.");
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                    var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

                    _logger.LogInformation("Executing data synchronization tasks.");
                    await SyncToMongoDB(mongoDatabase, dbContext);
                    await SyncToRedis(redis, dbContext);

                    _logger.LogInformation("Data synchronization tasks completed. Waiting for next cycle.");
                }
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task SyncToMongoDB(IMongoDatabase mongoDatabase, ApplicationDbContext dbContext)
        {
            var mongoCollection = mongoDatabase.GetCollection<MongoSale>("Sales");
            var salesToSync = dbContext.Sales.ToList();

            foreach (var sale in salesToSync)
            {
                var existingSale = mongoCollection.Find(Builders<MongoSale>.Filter.Eq(s => s.InvoiceNo, sale.InvoiceNo)).FirstOrDefault();
                if (existingSale == null || !SaleEquals(existingSale, sale, dbContext))
                {
                    var filter = Builders<MongoSale>.Filter.Eq(s => s.InvoiceNo, sale.InvoiceNo);
                    var update = Builders<MongoSale>.Update
                        .Set(s => s.StockCode, sale.StockCode)
                        .Set(s => s.Quantity, sale.Quantity)
                        .Set(s => s.UnitPrice, sale.UnitPrice)
                        .Set(s => s.CustomerID, sale.CustomerID)
                        .Set(s => s.CountryName, salesToSync.FirstOrDefault(c => c.CountryID == sale.CountryID)?.Country.CountryName)
                        .Set(s => s.InvoiceDate, sale.Dates.InvoiceDate);

                    await mongoCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
                }
            }
            _logger.LogInformation("Data successfully synchronized to MongoDB.");
        }

        private async Task SyncToRedis(IConnectionMultiplexer redis, ApplicationDbContext dbContext)
        {
            var db = redis.GetDatabase();
            var salesToSync = dbContext.Sales.ToList();

            foreach (var sale in salesToSync)
            {
                
                string saleid = Guid.NewGuid().ToString();

                var key = $"sale:{saleid}";
                var existingSaleJson = await db.StringGetAsync(key);
                var existingSale = existingSaleJson.HasValue ? Newtonsoft.Json.JsonConvert.DeserializeObject<RedisSale>(existingSaleJson) : null;

                if (existingSale == null || !SaleEquals(existingSale, sale))
                {
                    var value = new RedisSale(
                        saleid,
                        sale.InvoiceNo,
                        sale.StockCode,
                        sale.Description,
                        sale.Quantity,
                        sale.UnitPrice,
                        sale.CustomerID,
                        sale.CountryID.ToString(),
                        sale.InvoiceDateID.ToString(),
                        sale.StockCode);

                    await db.StringSetAsync(key, Newtonsoft.Json.JsonConvert.SerializeObject(value));
                }
            }
            _logger.LogInformation("Data successfully synchronized to Redis.");
        }

        private bool SaleEquals(MongoSale mongoSale, Sale sqlSale, ApplicationDbContext dbContext)
        {
            return mongoSale.StockCode == sqlSale.StockCode &&
                   mongoSale.Quantity == sqlSale.Quantity &&
                   mongoSale.UnitPrice == sqlSale.UnitPrice &&
                   mongoSale.CustomerID == sqlSale.CustomerID &&
                   mongoSale.CountryName == dbContext.Countries.FirstOrDefault(c => c.CountryID == sqlSale.CountryID)?.CountryName;
        }

        private bool SaleEquals(RedisSale redisSale, Sale sqlSale)
        {
            return redisSale.StockCode == sqlSale.StockCode &&
                   redisSale.Quantity == sqlSale.Quantity &&
                   redisSale.UnitPrice == sqlSale.UnitPrice &&
                   redisSale.CustomerID == sqlSale.CustomerID &&
                   redisSale.CountryID == sqlSale.CountryID.ToString() &&
                   redisSale.InvoiceDateID == sqlSale.InvoiceDateID.ToString();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Nightly Data Sync Service is stopping.");

            if (_executingTask == null)
            {
                return;
            }

            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }
    }
}
