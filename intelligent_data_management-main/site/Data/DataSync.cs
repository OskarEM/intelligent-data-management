using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Site.Data;
using Site.Models;
using StackExchange.Redis;

public class DataSyncService 
{
    private readonly ILogger<DataSyncService> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMongoDatabase _mongoDatabase;

    public DataSyncService(
        ILogger<DataSyncService> logger,
        IConnectionMultiplexer redis,
        IMongoClient client,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _redis = redis;
        _mongoDatabase = client.GetDatabase("MyAppDatabase");
        
        _dbContext = dbContext;
    }

    public async Task SyncToMongoDB(Sale model)
    {
        var mongoCollection = _mongoDatabase.GetCollection<MongoSale>("Sales");

        try
        {
            // Create a filter to find the sale in MongoDB based on the InvoiceNo
            var filter = Builders<MongoSale>.Filter.Eq(s => s.InvoiceNo, model.InvoiceNo);

            // Create an update definition with the data to be updated or inserted
            var update = Builders<MongoSale>.Update
                .Set(s => s.StockCode, model.StockCode)
                .Set(s => s.Quantity, model.Quantity)
                .Set(s => s.UnitPrice, model.UnitPrice)
                .Set(s => s.CustomerID, model.CustomerID)
                .Set(s => s.CountryName, _dbContext.Countries.FirstOrDefault(c => c.CountryID == model.CountryID)?.CountryName)
                .Set(s => s.InvoiceDate, model.Dates.InvoiceDate);

            // Use UpdateOneAsync with upsert option true, which will insert the document if it doesn't exist
            await mongoCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });

            _logger.LogInformation($"Data for InvoiceNo {model.InvoiceNo} synchronized to MongoDB.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error synchronizing to MongoDB for InvoiceNo {model.InvoiceNo}: {ex.Message}", ex);
        }
    }


    public async Task SyncToRedis(Sale model)
    {
        var db = _redis.GetDatabase();
        try {
            // Sync Sale
            string saleid = Guid.NewGuid().ToString();

            var saleKey = $"sale:{saleid}";
            var saleValue = new RedisSale(saleid,
                model.InvoiceNo, 
                model.StockCode, 
                model.Description, 
                model.Quantity, 
                model.UnitPrice,
                model.CustomerID, 
                model.CountryID.ToString(), 
                model.InvoiceDateID.ToString(),
                model.StockCode);
            await db.StringSetAsync(saleKey, JsonConvert.SerializeObject(saleValue));

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error synchronizing to Redis for InvoiceNo {model.InvoiceNo}: {ex.Message}", ex);
            }
        }

    
}
