using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace OnamaFrontendApi.Lib
{
  public class CacheService : ICacheService
  {
    protected const string REDIS_HOSTNAME = "onama-frontend-redis";
    protected const int REDIS_PORT = 6379;
    protected readonly IConfiguration Configuration;
    protected readonly ILogger Logger;
    protected string RedisHost { get; set; }
    protected int RedisPort { get; set; }
    protected IDatabase RedisConnection { get; set; }

    public CacheService(IConfiguration configuration, ILogger<CacheService> logger)
    {
      this.Configuration = configuration;
      this.Logger = logger;
      Configure();
      try 
      {
        ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect($"{RedisHost}:{RedisPort}");
        RedisConnection = muxer.GetDatabase();
      }
      catch(Exception ex)
      {
        Logger.LogWarning(6379, ex, "Cannot connect to redis, not using Cache");
      }

    }
    public string Get(string key)
    {
      if(RedisConnection == null)
      {
        return null;
      }

      string jsonResponse = null;
      try 
      {
        var cachedResponse = RedisConnection.StringGet(key);
        if(cachedResponse != RedisValue.Null)
        {
          jsonResponse = cachedResponse.ToString();
        } 
      }
      catch(Exception ex)
      {
        Logger.LogWarning(6379, ex, "Connection to redis lost, not using Cache");
      }
      return jsonResponse;
    }
    public void Put(string key, string payload)
    {
      if(RedisConnection != null)
      {
        try{
          RedisConnection.StringSet(key, payload);
        }
        catch(Exception ex)
        {
          Logger.LogWarning(6379, ex, "Could not write to redis, response will not be cached");
        }
      }
    }
    private void Configure()
    {
      RedisHost = REDIS_HOSTNAME;
      RedisPort = REDIS_PORT;
      if (Configuration["Onama:RedisHost"] != null)
      {
        RedisHost = Configuration["Onama:RedisHost"];
      }
      if (Configuration["Onama:RedisPort"] != null)
      {
        int port = REDIS_PORT; 

        if (int.TryParse(Configuration["Onama:RedisPort"], out port))
        {
          RedisPort = port;
        }
      }
    }
  }
}