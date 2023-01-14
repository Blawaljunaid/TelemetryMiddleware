using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.Connections
{
    public class RedisManager
    {
        private static readonly Lazy<ConnectionMultiplexer> LazyConnection;
        private static string ConnectionString = ConfigurationManager.AppSettings["RedisConnection"];

        static RedisManager()
        {
            var configurationOptions = ConfigurationOptions.Parse(ConnectionString);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.KeepAlive = 10;
            configurationOptions.ConfigCheckSeconds = 0;
            configurationOptions.AllowAdmin = true;

            //var commandMap = CommandMap.Create(new HashSet<string>
            //{ // EXCLUDE a few commands
            //    "INFO", "CONFIG", "CLUSTER",
            //     "SUBSCRIBE", "UNSUBSCRIBE",
            //    "PING", "ECHO", "CLIENT"
            //}, available: false);

            //configurationOptions.CommandMap = commandMap;

          

            LazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(configurationOptions));
        }


        public static ConnectionMultiplexer Connection => LazyConnection.Value;

        public static IDatabase GetDB => Connection.GetDatabase();

       // public static IServer GetServer => Connection.GetServer(ConnectionString);

    }
}
