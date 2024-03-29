﻿using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class EODCoordinatorProcessor : BaseProcessor
    {
        public EODCoordinatorProcessor(Loader loader) : base(loader, "CriticalServicesMonitor", "Fingrid.BankOne.Corebanking.EODCoordinator.Live")
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
                int noOfRunimm, noOfRunST,noOfFailed;
                string[] inputParam = message.Split(',');
                int.TryParse(inputParam[0].Trim(), out noOfRunimm);
                int.TryParse(inputParam[1].Trim(), out noOfRunST);
                int.TryParse(inputParam[2].Trim(), out noOfFailed);
                Logger.Log("Parsed date is {0}", DateTime.Parse(inputParam[3].Trim()));

                EODCoordinatorobj obj = new EODCoordinatorobj
                {
                    Date = DateTime.Parse(inputParam[3].Trim()),
                    NoOfRunImmediately = noOfRunimm,
                    NoOfRunAtSpecifiedTime = noOfRunST,
                    NoOfFailed = noOfFailed,


                };
                Logger.Log("EODCoordinator Object {0},{1},{2},{3} converted.", obj.Date, obj.NoOfRunImmediately, obj.NoOfRunAtSpecifiedTime, obj.NoOfFailed);

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

        private Point GeneratePoint(EODCoordinatorobj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "EODCoordinator",
                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "NoOfRunImmediately", currentObj.NoOfRunImmediately },
                    { "NoOfRunAtSpecifiedTime", currentObj.NoOfRunAtSpecifiedTime },
                    { "noOfFailed", currentObj.NoOfFailed},
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        public class EODCoordinatorobj
        {
            public int NoOfRunImmediately { get; set; }
            public DateTime Date { get; set; }
            public int NoOfRunAtSpecifiedTime { get; set; }
            public int NoOfFailed { get; set; }

        }

    }
}
