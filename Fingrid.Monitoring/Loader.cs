using Fingrid.Monitoring.Utility;
using Fingrid.Monitoring.Connections;
using InfluxDB.Net;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class Loader
    {
        InfluxDb influxDbClient;
        ConnectionMultiplexer redis;
        public async Task LoadAll()
        {

            influxDbClient = InfluxDBManager.Connection; // new InfluxDb(ConfigurationManager.AppSettings["InfluxDBUrl"], ConfigurationManager.AppSettings["InfluxDBUserName"],ConfigurationManager.AppSettings["InfluxDBPassword"]);


            redis = RedisManager.Connection; //ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings["RedisConnection"]);

            EventStoreConnector.Connect(ConfigurationManager.AppSettings["EventStoreConnection"]);

            //cacheClient = new StackExchangeRedisCacheClient(new StackExchange.Redis.Extensions.Jil.JilSerializer());
            Logger.Log("Connection is {0}established to ", redis.IsConnected ? "" : "NOT ");

            InfluxDB.Net.Models.Pong pong = await influxDbClient.PingAsync();
            Logger.Log("Connection is {0}established to Influx Db.", pong.Success ? "" : "NOT ");

        }

        public InfluxDb InfluxDb { get { return this.influxDbClient; } }
        public ConnectionMultiplexer RedisConnection { get { return this.redis; } }

    }
}
