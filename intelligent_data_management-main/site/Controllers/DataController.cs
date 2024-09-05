using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using Site.Data;
using Site.Models;

namespace Site.Controllers
{
    public class DataController : Controller
    {
        private readonly ILogger<DataController> _logger;
        private readonly ApplicationDbContext _dbContext; // For PostgreSQL
        private readonly IMongoClient _mongoClient; // For MongoDB
        private readonly IConnectionMultiplexer _redisConnection; // For Redis

        public DataController(ILogger<DataController> logger,
                              ApplicationDbContext dbContext,
                              IMongoClient mongoClient,
                              IConnectionMultiplexer redisConnection) // Inject the Redis connection
        {
            _logger = logger;
            _dbContext = dbContext;
            _mongoClient = mongoClient;
            _redisConnection = redisConnection;
        }

        public async Task<IActionResult> PostgresData()
        {
            var data = await _dbContext.Stubs.ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> MongoData()
        {
            var database = _mongoClient.GetDatabase("MyAppDatabase");
            var collection = database.GetCollection<Stub>("stubs");
            var data = await collection.Find(_ => true).ToListAsync();
            return View("MongoData", data);
        }

        public async Task<IActionResult> RedisData() // New method for Redis data
        {
            var db = _redisConnection.GetDatabase();
            // Example: Fetch a value by key. Adjust based on your Redis data structure
            var value = await db.StringGetAsync("your_key_here");

            // Assume 'value' is a serialized object and needs to be deserialized
            // This part depends on how you store your data in Redis
            // Here's an example if 'value' is a simple string representing a Stub model
            // You'd replace this with your actual model deserialization
            var data = new Stub(); // Placeholder for deserialization logic

            return View("RedisData", new[] { data }); // Adjust view name and model as necessary
        }
    }
}
