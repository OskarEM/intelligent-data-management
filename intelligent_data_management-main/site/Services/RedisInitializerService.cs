using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Site.Data;

public class RedisInitializerService : IHostedService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly string _csvPath;

    public RedisInitializerService(IConnectionMultiplexer connectionMultiplexer, IConfiguration configuration)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _csvPath = configuration["CsvData:Path"];
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var redisDbInitializer = new RedisDbInitializer(_connectionMultiplexer, _csvPath);
        await redisDbInitializer.InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
