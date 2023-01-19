using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class TestPublishRed
    {
        ConnectionMultiplexer redis = null;
        //ICacheClient cacheClient = null;
        string channel;
        public TestPublishRed(string channel)
        {
            this.channel = channel;
            //cacheClient = new StackExchangeRedisCacheClient(new StackExchange.Redis.Extensions.Jil.JilSerializer());
            redis = ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings["RedisConnection"]);
        }

        public void Publish(string info)
        {
            //cacheClient.Publish(this.channel, info);
            redis.GetSubscriber().Publish(this.channel, info);
        }
    }
}
