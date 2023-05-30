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
 
    public class BaseMobileUptime : BaseProcessor
    {
        ConcurrentDictionary<string, BaseMobileUptimeobj> objDict = null;
        public BaseMobileUptime(Loader loader, string Channel) : base(loader, "CriticalServicesMonitor", Channel)
        {
            this.objDict = new ConcurrentDictionary<string, BaseMobileUptimeobj>();
        }


        protected override async void BreakMessageAndFlush(string message)
        {

            try
            {

                Logger.Log("");
                Logger.Log(message);
                //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
                message = message.Trim('"', ' ');
                Logger.Log(message);
                string[] inputParam = message.Split(',');

                BaseMobileUptimeobj obj = new BaseMobileUptimeobj
                {
                    UniqueID = inputParam[0].Trim(),
                    Date = DateTime.ParseExact(inputParam[1].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    AppName = inputParam[2].Trim(),
                    IsResponse = Convert.ToBoolean(inputParam[3].Trim()),
                    ResponseCode = inputParam.Length> 4? inputParam[4].Trim() : "N/A",
                    
                };



                Logger.Log("BaseMobileUptime Object {0},{1},{2},{3},{4} converted.", obj.UniqueID, obj.Date, obj.AppName, obj.IsResponse, obj.ResponseCode);


                if (!obj.IsResponse)
                {
                    Logger.Log("Recording Request object with id: {0}", obj.UniqueID);
                    this.objDict.AddOrUpdate(obj.UniqueID, obj, (val1, val2) => obj);
                    return;
                }

                BaseMobileUptimeobj initialObj = null;
                Logger.Log("Recording response object with id: {0}", obj.UniqueID);
                if (!this.objDict.TryRemove(obj.UniqueID, out initialObj))
                {
                    Logger.Log("Could not find response object with id: {0}", obj.UniqueID);
                    return;
                }

                Logger.Log("Sending response and request object to generate point to send to influx");
                var pointToWrite = GeneratePoint(obj,initialObj);
                Logger.Log("Generate point complete");

                try
                {
                    Console.WriteLine("Start writing to influx");
                    var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                    Console.WriteLine("DONE writing to influx");
                }
                catch (Exception ex)
                {
                    Logger.Log("Issue writing to influx DB");
                    Logger.Log(ex.Message);
                    Environment.Exit(1); // Exit with a non-zero status code so that the docker container can restart
                    // if for any reason we switch back to using this as a windows service
                    // you will have to comment that Exit() line of code in all the other files to avoid service
                    // crashing anytime inlfux has connection issue
                }


            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                //Logger.Log(ex.InnerException?.Message);
                Logger.Log("THE ERROR STACK: " + ex.StackTrace);
            }

        }


        private Point GeneratePoint(BaseMobileUptimeobj currentObj, BaseMobileUptimeobj initialObj)
        {
            double totalMillisecs = currentObj.Date.Subtract(initialObj.Date).TotalMilliseconds;
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "MobileUptime",
                Tags = new Dictionary<string, object>()
                {

                    {"Environment", currentObj.AppName },
                    {"ResponseCode", currentObj.ResponseCode },
                    {"TechnicalSuccess", IsSuccessful(currentObj.ResponseCode) ? "Successful" : "Failed" },


                },


                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "SuccessCnt", IsSuccessful(currentObj.ResponseCode) ? 1 : 0 },
                    {"TimeTaken",  totalMillisecs},
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }

        private bool IsSuccessful(string responseCode)
        {
            if (!String.IsNullOrEmpty(responseCode) && responseCode == "00")
            {
                return true;
            }
            return false;
        }

        public class BaseMobileUptimeobj
        {
            public string UniqueID { get; set; }
            public DateTime Date { get; set; }
            public string AppName { get; set; }
            public string ResponseCode { get; set; }
            public bool IsResponse { get; set; }

        }
    }
}
