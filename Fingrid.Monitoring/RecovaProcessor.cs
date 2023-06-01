using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System.Collections.Concurrent;

namespace Fingrid.Monitoring
{
    public class RecovaProcessor : BaseProcessor
    {
        ConcurrentDictionary<string, RecovaProcessorObj> objDict = null;

        public RecovaProcessor(Loader loader)
           : base(loader, "RecovaProcessor", "recova_middleware_transactions-production")
        {
            this.objDict = new ConcurrentDictionary<string, RecovaProcessorObj>();

        }
        public class RecovaProcessorObj
        {
            public string RequestId { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public string InstitutionCode { get; set; }
            public string InstitutionName { get; set; }
            public string? Amount { get; set; }
            public string TransactionType { get; set; }
            public string Status { get; set; }
            public DateTime CreateDate { get; set; }
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

                //"testbackt23244,01,Sorry, we are unable to process your request at this time. Please try again or report this issue., 014,Sterling Bank,0.0,Partial Debit Transfer,Failed,2023-05-12T18:31:49.6106884 01:00"
                
                //RecovaProcessorObj obj = JsonConvert.DeserializeObject<RecovaProcessorObj>(message);

                RecovaProcessorObj obj = new RecovaProcessorObj
                {
                    RequestId = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    ResponseCode = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    ResponseMessage = !string.IsNullOrEmpty(inputParam[2]) ? inputParam[2].Trim() : "NA",
                    InstitutionCode = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    InstitutionName = !string.IsNullOrEmpty(inputParam[4]) ? inputParam[4].Trim() : "NA",
                    Amount = !string.IsNullOrEmpty(inputParam[5]) ? inputParam[5].Trim() : "NA",
                    TransactionType = !string.IsNullOrEmpty(inputParam[6]) ? inputParam[6].Trim() : "NA",
                    Status = !string.IsNullOrEmpty(inputParam[7]) ? inputParam[7].Trim() : "NA",
                    CreateDate = DateTime.ParseExact(inputParam[8].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                };


                 Logger.Log("RecovaProcessor Object {0},{1},{2},{3},{4},{5} converted.", obj.RequestId, obj.ResponseCode, obj.TransactionType, obj.Status, obj.InstitutionName, obj.ResponseMessage);

                Console.WriteLine("Sending object to generate point to send to influx");
                var pointToWrite = GeneratePoints(obj);
                Console.WriteLine("Generate point complete");

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

        private Point GeneratePoints(RecovaProcessorObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx",
                Tags = new Dictionary<string, object>()
                {

                    {"ResponseCode", currentObj.ResponseCode },
                    {"InstitutionName", currentObj.InstitutionName },
                    {"TransactionType", currentObj.TransactionType },
                    {"ResponseMessage", currentObj.ResponseMessage },
                    {"Status", currentObj.Status },

                },
                Fields = new Dictionary<string, object>()
                {
                    //{"Time", currentObj.CreateDate.Subtract(initialObj.Time).TotalMilliseconds },
                    {"Time", currentObj.CreateDate.Millisecond },
                    {"FailedCnt", currentObj.Status == "Failed" ? 1 : 0},
                    {"TotalCnt", 1},
                    {"Amount", currentObj.Amount },
                    {"UniqueId", currentObj.RequestId },
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


    }
}
