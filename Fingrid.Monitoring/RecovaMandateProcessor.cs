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
    public class RecovaMandateProcessor : BaseProcessor
    {
        ConcurrentDictionary<string, RecovaMandateProcessorObj> objDict = null;
        Dictionary<string, Institution> institutionsDict = null;

        public RecovaMandateProcessor(Loader loader, List<Institution> institutions)
           : base(loader, "RecovaProcessor", "mandates_recova_mandate-staging")
        {
            this.objDict = new ConcurrentDictionary<string, RecovaMandateProcessorObj>();

            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Id))
                    this.institutionsDict.Add(institution.Id.Trim(), institution);
            }

        }
        public class RecovaMandateProcessorObj
        {
            public string UniqueId { get; set; }
            public string? InstitutionId { get; set; }
            public string? LoanReference { get; set; }
            public string? MandateRequestReference { get; set; }
            public string? BVN { get; set; }
            public string? LoanAmount { get; set; }
            public string? RequestStatus { get; set; }
            public string? MandateType { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {
                Logger.Log("");
                Logger.Log(message);
                message = message.Trim('"', ' ');
                Logger.Log(message);

                string[] inputParam = message.Split(',');

                RecovaMandateProcessorObj obj = new RecovaMandateProcessorObj
                {
                    UniqueId = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    InstitutionId = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    LoanReference = !string.IsNullOrEmpty(inputParam[2]) ? inputParam[2].Trim() : "NA",
                    MandateRequestReference = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    BVN = !string.IsNullOrEmpty(inputParam[4]) ? inputParam[4].Trim() : "NA",
                    LoanAmount = !string.IsNullOrEmpty(inputParam[5]) ? inputParam[5].Trim() : "NA",
                    RequestStatus = !string.IsNullOrEmpty(inputParam[6]) ? inputParam[6].Trim() : "NA",
                    MandateType = !string.IsNullOrEmpty(inputParam[7]) ? inputParam[7].Trim() : "NA",
                    CreatedDate = DateTime.ParseExact(inputParam[8].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                };


                Logger.Log("RecovaMandateProcessor Object {0},{1},{2},{3},{4},{5} converted.", obj.UniqueId, obj.InstitutionId, obj.LoanReference, obj.MandateRequestReference, obj.LoanAmount, obj.CreatedDate);

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

        private Point GeneratePoints(RecovaMandateProcessorObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "mandates",
                Tags = new Dictionary<string, object>()
                {

                    {"RequestStatus", currentObj.RequestStatus! },
                    {"InstitutionName", InstitutionInfo.GetRecovaInstitutionName(currentObj.InstitutionId!,this.institutionsDict) },
                    {"MandateType", currentObj.MandateType! },

                },
                Fields = new Dictionary<string, object>()
                {
                    {"TotalCnt", 1},
                    {"InstitutionId", currentObj.InstitutionId! },
                    {"LoanReference", currentObj.LoanReference! },
                    {"MandateRequestReference", currentObj.MandateRequestReference! },
                    {"LoanAmount", currentObj.LoanAmount! },
                    {"UniqueId", currentObj.UniqueId },
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


    }
}
