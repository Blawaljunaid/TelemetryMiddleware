using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
   
    public class BatchAccountStatementGeneration : BaseProcessor
    {
        public BatchAccountStatementGeneration(Loader loader) : base(loader, "CriticalServicesMonitor", "Fingrid.BankOne.Corebanking.BatchAccountStatement.Generation")
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
                int noOfRunimm, noOfRunST, noOfFailed;
                string[] inputParam = message.Split(',');
                int.TryParse(inputParam[0].Trim(), out noOfRunimm);
                int.TryParse(inputParam[1].Trim(), out noOfRunST);
                int.TryParse(inputParam[2].Trim(), out noOfFailed);

                BatchAccountStatementGenerationobj obj = new BatchAccountStatementGenerationobj
                {
                    noOfLoggedBatchAccountStatementStatus = noOfRunimm,
                    noOfProcessingBatchAccountStatementStatus = noOfRunST,
                    noOfFinishedBatchAccountStatementStatus = noOfFailed,


                };

                Logger.Log("BatchAccountStatementGeneration Object {0},{1},{2} converted.", obj.noOfLoggedBatchAccountStatementStatus, obj.noOfProcessingBatchAccountStatementStatus, obj.noOfFinishedBatchAccountStatementStatus);


                Logger.Log("Generate point to write to influx");
                var pointToWrite = GeneratePoint(obj);
                Logger.Log("Generate point complete");

                Logger.Log("Start wriiting to influx");
                //Point is then passed into Client.WriteAsync method together with the database name:
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                Logger.Log("DONE wriiting to influx");


            }
            catch (Exception ex)
            {

                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point GeneratePoint(BatchAccountStatementGenerationobj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "BatchAccountStatementGeneration",
                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "noOfFinishedBatchAccountStatementStatus", currentObj.noOfFinishedBatchAccountStatementStatus },
                    { "noOfLoggedBatchAccountStatementStatus", currentObj.noOfLoggedBatchAccountStatementStatus },
                    { "noOfProcessingBatchAccountStatementStatus", currentObj.noOfProcessingBatchAccountStatementStatus},
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        public class BatchAccountStatementGenerationobj
        {
            public int noOfLoggedBatchAccountStatementStatus { get; set; }
            public DateTime Date { get; set; }
            public int noOfProcessingBatchAccountStatementStatus { get; set; }
            public int noOfFinishedBatchAccountStatementStatus { get; set; }

        }

    }
}
