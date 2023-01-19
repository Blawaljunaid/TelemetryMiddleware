using InfluxDB.Net;
using InfluxDB.Net.Infrastructure.Influx;
using InfluxDB.Net.Models;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public abstract class BaseProcessor
    {
        string channelToSubscribeTo;
        protected string databaseName;
        public BaseProcessor(Loader loader, string databaseName, string channelToSubscribeTo)
        {
            Console.WriteLine("Base Processor");
            this.influxDbClient = loader.InfluxDb;
            this.redis = loader.RedisConnection;
            Console.WriteLine("Redis Connected");
            this.channelToSubscribeTo = channelToSubscribeTo;
            Console.WriteLine("channelToSubscribeTo");
            this.databaseName = databaseName;
            Console.WriteLine("databaseName");
        }

        ConnectionMultiplexer redis = null;
        //ICacheClient cacheClient = null;
        protected InfluxDb influxDbClient = null;

        public async void StartListening()
        {

            try
            {
                //InfluxDbApiResponse response = await influxDbClient.CreateDatabaseAsync(databaseName);

                Trace.TraceInformation("Listening by : {0}", this.GetType());
                Console.WriteLine("Listening by : {0}", this.GetType());
                bool shouldSubscribeToEventStore = Convert.ToBoolean(ConfigurationManager.AppSettings["shouldSubscribeToEventStore"] ?? "true");
                bool shouldSubscribeToRedis = Convert.ToBoolean(ConfigurationManager.AppSettings["shouldSubscribeToRedis"] ?? "false");

                if (shouldSubscribeToRedis)
                {
                    redis.GetSubscriber().Subscribe(this.channelToSubscribeTo, (channel, message) =>
                    {
                        try
                        {

                            BreakMessageAndFlush(message);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceInformation(ex.Message + ex.StackTrace);
                        }

                    });
                }

                if (shouldSubscribeToEventStore)
                {
                    await EventStoreConnector.GetConnection().SubscribeToStreamAsync(this.channelToSubscribeTo, false,
                    (subscription, recievedEvent) =>
                    {
                        try
                        {
                            BreakMessageAndFlush(Encoding.UTF8.GetString(recievedEvent.Event.Data));
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceInformation(ex.Message + ex.StackTrace);
                        }
                        return null;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when Listening by : {0}", this.GetType());
                Console.WriteLine("Error when Listening by : {0}", this.GetType());
                Console.WriteLine(ex.Message);
                throw ex;
            }
            Console.WriteLine("Done Listening by : {0}", this.GetType());
            System.Console.WriteLine("Done Listening by : {0}", this.GetType());
        }

        protected virtual async void BreakMessageAndFlush(string message)
        {

        }




        public void Stop()
        {
            //if (redis != null)
            //{
            //    redis.Close();
            //}
        }


    }
}



//public class RedisPubSub
//{
//    static ICacheClient cacheClient = null;
//    static IRedisCacheStorage cacheStorage = null;
//    private string channelKey = "Fingrid.Channels";
//    public RedisPubSub()
//    {
//        if (cacheClient == null) cacheClient = new StackExchangeRedisCacheClient(new StackExchange.Redis.Extensions.Jil.JilSerializer());
//        if (cacheStorage == null) cacheStorage = new Fingrid.Infrastructure.Common.Caching.Redis.RedisExtendedCacheStorage(cacheClient);
//    }
//    public void Publish(string name, string message)
//    {
//        cacheStorage.CacheClient.Publish(name, "cacheStorage " + message);
//        cacheClient.Publish(name, "cacheClient " + message);
//    }
//    public void Subscribe(string name, Action<string> handler)
//    {
//        cacheClient.Subscribe(name, handler);
//    }
//    public List<string> GetAllChannels()
//    {
//        var results = cacheClient.Database.SetMembers(channelKey)?.Select(y => y.ToString()).ToList();
//        return results ?? new List<string>();
//    }
//}
