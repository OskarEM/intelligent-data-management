

using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Site.Models;
using System.Linq;

namespace Site.Data
{
    

    public class MyRedisService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public MyRedisService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public async Task<string> GetValueAsync(string key)
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.StringGetAsync(key);
        }

        public async Task SetValueAsync(string key, string value, TimeSpan? expiry = null)
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.StringSetAsync(key, value, expiry);
        }

        
    }
}
