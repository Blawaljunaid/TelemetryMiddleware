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
    public class RecovaAuthProcessor : BaseProcessor
    {
        ConcurrentDictionary<string, RecovaAuthProcessorObj> objDict = null;

        public RecovaAuthProcessor(Loader loader)
           : base(loader, "RecovaProcessor", "recova_auth-production")
        {
            this.objDict = new ConcurrentDictionary<string, RecovaAuthProcessorObj>();

        }
        public class RecovaAuthProcessorObj
        {
            public string UniqueId { get; set; }
            public string? Category { get; set; }
            public bool? LoginStatus { get; set; }
            public string? InstitutionCode { get; set; }
            public string? UserId { get; set; }
            public string? Username { get; set; }
            public string? UserRoleId { get; set; }
            public DateTime CreatedDate { get; set; }
            public string? InstitutionName { get; set; }
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

                RecovaAuthProcessorObj obj = new RecovaAuthProcessorObj
                {
                    UniqueId = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    Category = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    LoginStatus = !string.IsNullOrEmpty(inputParam[2]) ? bool.Parse(inputParam[2].Trim()) : null,
                    InstitutionCode = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    UserId = !string.IsNullOrEmpty(inputParam[4]) ? inputParam[4].Trim() : "NA",
                    Username = !string.IsNullOrEmpty(inputParam[5]) ? inputParam[5].Trim() : "NA",
                    UserRoleId = !string.IsNullOrEmpty(inputParam[6]) ? inputParam[6].Trim() : "NA",
                    CreatedDate = DateTime.ParseExact(inputParam[7].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    InstitutionName = !string.IsNullOrEmpty(inputParam[8]) ? inputParam[8].Trim() : "NA",
                };


                Logger.Log("RecovaAuthProcessor Object {0},{1},{2},{3},{4},{5} converted.", obj.UniqueId, obj.Category, obj.Username, obj.LoginStatus, obj.InstitutionName, obj.CreatedDate);

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

        private Point GeneratePoints(RecovaAuthProcessorObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Auth",
                Tags = new Dictionary<string, object>()
                {

                    {"LoginStatus", currentObj.LoginStatus! },
                    {"InstitutionName", currentObj.InstitutionName! },
                    {"UserRoleId", currentObj.UserRoleId! },
                    {"Category", currentObj.Category! },

                },
                Fields = new Dictionary<string, object>()
                {
                    {"FailedCnt", currentObj.LoginStatus == false ? 1 : 0},
                    {"TotalCnt", 1},
                    {"InstitutionCode", currentObj.InstitutionCode! },
                    {"UserId", currentObj.UserId! },
                    {"Username", currentObj.Username! },
                    {"UniqueId", currentObj.UniqueId },
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


    }
}
