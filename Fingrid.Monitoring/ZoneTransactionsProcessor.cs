using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class ZoneTransactionsProcessor : BaseProcessor
    {
        ConcurrentDictionary<string, TestZoneObj> objDict = null;
        public ZoneTransactionsProcessor(Loader loader)
            : base(loader, "ZoneTransactions", "__Monitoring.Zone.Transactions.Generic.Live")
        {
            this.objDict = new ConcurrentDictionary<string, TestZoneObj>();
        }

        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {
                Logger.Log(message);
                //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
                message = message.Trim('"', ' ');
                Logger.Log(message);

                string[] inputParam = message.Split(',');
                TestZoneObj obj = new TestZoneObj
                {
                    UniqueId = inputParam[0].Trim(),
                    IsPingdom = inputParam[1].Trim() == "Pingdom" ? 1 : 0,
                    Response = inputParam[2].Trim(),
                    FailureCode = inputParam[3].Trim(),
                    //TransactionTime = DateTime.ParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    Number = inputParam[5].Trim()
                };

                DateTime responseDate = DateTime.Now;
                if (DateTime.TryParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out responseDate))
                {
                    obj.TransactionTime = responseDate;
                }
                else
                {
                    Logger.Log("Error parsing date {0}", inputParam[4].Trim());
                    return;
                }

                if (obj.Number == "1")
                {
                    this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                    return;
                }

                TestZoneObj initialObj = null;
                if (!this.objDict.TryRemove(obj.UniqueId, out initialObj))
                {
                    return;
                }


                var pointToWrite = GeneratePoint(obj, initialObj);
                //Point is then passed into Client.WriteAsync method together with the database name:

                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
            }
        }

        private Point GeneratePoint(TestZoneObj currentObj, TestZoneObj initialObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {

                    {"FailureCode", currentObj.FailureCode },
                    {"IsPingdom", currentObj.IsPingdom },
                    {"Response", currentObj.Response },
                },

                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "Time", currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds },
                    { "SuccessCnt", currentObj.Response == "Successful" ? 1 : 0 },
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        public class TestZoneObj
        {
            public string Response { get; set; }

            public int IsPingdom { get; set; }

            public string FailureCode { get; set; }
            public DateTime TransactionTime { get; set; }
            public string UniqueId { get; set; }

            public string Number { get; set; }
        }

    }
}
