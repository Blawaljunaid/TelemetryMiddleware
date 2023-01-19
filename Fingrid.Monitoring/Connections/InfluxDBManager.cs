using InfluxDB.Net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.Connections
{
   public  class InfluxDBManager
    {

        private static readonly Lazy<InfluxDb> LazyInflux;

        private static string InfluxDBPassword = ConfigurationManager.AppSettings["InfluxDBPassword"];
        private static string InfluxDBUrl = ConfigurationManager.AppSettings["InfluxDBUrl"];
        private static string InfluxDBUserName = ConfigurationManager.AppSettings["InfluxDBUserName"];


        static InfluxDBManager()
        {
            LazyInflux = new Lazy<InfluxDb>(() => new InfluxDb(InfluxDBUrl,InfluxDBUserName, InfluxDBPassword));

        }   


        public static InfluxDb Connection => LazyInflux.Value;




    }
}
