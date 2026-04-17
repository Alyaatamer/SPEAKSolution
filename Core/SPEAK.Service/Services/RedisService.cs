using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace SPEAK.Service.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;

        public RedisService(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("Redis:ConnectionString")
                ?? throw new InvalidOperationException("Redis connection string is missing.");

            var options = ConfigurationOptions.Parse(connectionString);

            options.AbortOnConnectFail = false;
            options.ConnectRetry = 5;
            options.ConnectTimeout = 10000;
            options.SyncTimeout = 10000;
            options.Ssl = true;
            options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;

            var redis = ConnectionMultiplexer.Connect(options);
            
            redis.ConnectionFailed += (s, e) => Console.WriteLine($"[Redis Error] Connection Failed: {e.Exception.Message}");
            redis.ConnectionRestored += (s, e) => Console.WriteLine("[Redis Info] Connection Restored");

            if (redis.IsConnected)
            {
                Console.WriteLine($"[Redis Info] Successfully connected to {redis.Configuration}");
            }
            else
            {
                Console.WriteLine($"[Redis Warning] Could not connect to Redis at startup. Status: {redis.GetStatus()}");
            }

            _db = redis.GetDatabase();
        }

        public async Task SetAsync(string key, string value, TimeSpan expiry)
        {
            await _db.StringSetAsync(key, value, expiry);
        }

        public async Task<string?> GetAsync(string key)
        {
            return await _db.StringGetAsync(key);
        }

        public async Task DeleteAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}
