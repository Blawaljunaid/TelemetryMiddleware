using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{


    public class BankOneAnniversaryNotificationProcessor : BaseProcessor
    {
        public BankOneAnniversaryNotificationProcessor(Loader loader) : base(loader, "CriticalServicesMonitor", "Fingrid.BankOne.Corebanking.BankOneAnniversaryNotification")
        {
            
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


                BankOneAnniversaryNotcnobj obj = new BankOneAnniversaryNotcnobj
                {
                    institutionName = inputParam[0].Trim(),
                    institutionCode = inputParam[1].Trim(),
                    Status = inputParam[2].Trim(),
                };
                if (string.IsNullOrEmpty(obj.institutionName)) obj.institutionName = "NA";
                if (string.IsNullOrEmpty(obj.institutionCode)) obj.institutionCode = "NA";
                if (string.IsNullOrEmpty(obj.Status)) obj.Status = "NA";

                Logger.Log("BankOneAnniversaryNotification Object {0},{1},{2} converted.", obj.institutionName, obj.institutionCode, obj.Status);


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

        private Point GeneratePoint(BankOneAnniversaryNotcnobj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "BankOneAnniversaryNotcn",
                Tags = new Dictionary<string, object>()
                {
                    { "institutionName", currentObj.institutionName },

                },
                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "institutionCode", currentObj.institutionCode },
                    { "Status", currentObj.Status},
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        public class BankOneAnniversaryNotcnobj
        {
            public string institutionName { get; set; }
            public DateTime Date { get; set; }
            public string institutionCode { get; set; }
            public string Status { get; set; }

        }

    }
}
