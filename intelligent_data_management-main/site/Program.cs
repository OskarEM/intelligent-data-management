using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using Site.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity; // Ensure this is included for Task

namespace Site
{
    public class Program
    {
        public static async Task Main(string[] args) // Change to async Task
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                
                // Get the UserManager and RoleManager also from the service provider
                var um = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var rm = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
                 await ApplicationDbInitializer.Initialize(dbContext,um , rm );

                var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
                MongoDbInitializer.Initialize(mongoClient);
                
                // Initialize Redis
                var redisConnection = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
                var redisInitializer = new RedisDbInitializer(redisConnection, "Data/data.csv"); // You need to pass CSV file path or handle it differently
                await redisInitializer.InitializeAsync(); // Correctly call and await the asynchronous Initialize method
            }

            await host.RunAsync(); // Ensure Run is awaited
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
