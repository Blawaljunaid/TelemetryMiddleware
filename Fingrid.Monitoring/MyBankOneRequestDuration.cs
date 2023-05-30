using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class MyBankOneRequestDuration : BaseProcessor
    {
        public MyBankOneRequestDuration(Loader loader) : base(loader, "CriticalServicesMonitor", "Fingrid.BankOne.Corebanking.RequestResponseDuration")
        {

        }

        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {

                Logger.Log("");
                Logger.Log(message);
                //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
                message = message.Trim('"', ' ', '\"');
                Logger.Log(message);
                float interval;
                string[] inputParam = message.Split(',');
                float.TryParse(inputParam[1].Trim(), out interval);

                MyBankOneRequestDurationObj obj = new MyBankOneRequestDurationObj
                {


                    URL = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    User = !string.IsNullOrEmpty(inputParam[2]) ? inputParam[2].Trim() : "NA",
                    Duration = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    TimeIn  = DateTime.Parse(inputParam[3].Trim().Trim('\\').Trim('"').Trim('\"').Trim('\\')),
                    TimeOut = DateTime.Parse(inputParam[4].Trim().Trim('\\').Trim('"').Trim('\"').Trim('\\')),
                    Action = !string.IsNullOrEmpty(inputParam[5]) ? inputParam[5].Trim() : "NA",
                    Interval = interval,


                };
                Logger.Log("MyBankOneRequestDuration Object {0},{1},{2},{3},{4},{5} converted.", obj.URL, obj.User, obj.Duration, obj.TimeIn, obj.TimeOut, obj.Action);

                Logger.Log("Generate point to write to influx");
                var pointToWrite = GeneratePoint(obj);
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
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point GeneratePoint(MyBankOneRequestDurationObj currentObj)
        {

            double totalMillisecs = currentObj.TimeIn.Subtract(currentObj.TimeOut).TotalMilliseconds;

            var pointToWrite = new Point()
               {
                   Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                   Measurement = "MyBankOneRequestDuration",
                   Tags = new Dictionary<string, object>()
                   {

                   { "URL", currentObj.URL},
                   {"Action", currentObj.Action },

                   },
                   Fields = new Dictionary<string, object>()
                   {
                   //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                   { "TimeIn", currentObj.TimeIn },
                   {"TimeOut", currentObj.TimeOut },
                   { "TimeTaken", totalMillisecs},
                   { "User", currentObj.User},
                   { "Duration", currentObj.Duration},
                   { "Interval", currentObj.Interval},
                   {"TotalCnt", 1},
                   },
                   Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
               };

                   return pointToWrite;
               }

        public class MyBankOneRequestDurationObj
        {
            public string URL { get; set; }
            public string Duration { get; set; }
            public string User { get; set; }
            public string Action { get; set; }
            public DateTime TimeIn { get; set; }
            public DateTime TimeOut { get; set; }
            public float Interval { get; set; }

        }

    }
}

