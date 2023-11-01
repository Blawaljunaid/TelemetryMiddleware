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
    public class IRestrictProcessor : BaseProcessor
    {
        ConcurrentDictionary<string, IRestrictProcessorObj> objDict = null;
        Dictionary<string, Institution> institutionsDict = null;

        public IRestrictProcessor(Loader loader, List<Institution> institutions)
           : base(loader, "IRestrictProcessor", "__Monitoring.Irestrict.Production")
        {
            this.objDict = new ConcurrentDictionary<string, IRestrictProcessorObj>();

            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Id))
                    this.institutionsDict.Add(institution.Id.Trim(), institution);
            }

        }
        public class IRestrictProcessorObj
        {
            public string UniqueId { get; set; }
            public string? InstitutionCode { get; set; }
            public bool IsResponse { get; set; }
            public string? APICode { get; set; }
            public string? IsSuccessful { get; set; }
            public string? ResponseDescription { get; set; }
            public DateTime TrnxDate { get; set; }
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

                IRestrictProcessorObj obj = new IRestrictProcessorObj
                {
                    UniqueId = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    InstitutionCode = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    IsResponse = bool.Parse(inputParam[2].Trim()),
                    APICode = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    IsSuccessful = !string.IsNullOrEmpty(inputParam[4]) ? inputParam[4].Trim() : "NA",
                    ResponseDescription = !string.IsNullOrEmpty(inputParam[5]) ? inputParam[5].Trim() : "NA",
                    TrnxDate = DateTime.ParseExact(inputParam[6].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                };


                Logger.Log("IRestrictProcessor Object {0},{1},{2},{3},{4},{5},{6} converted.", obj.UniqueId, obj.InstitutionCode, obj.IsResponse, obj.APICode, obj.IsSuccessful,obj.ResponseDescription, obj.TrnxDate);


                if (obj.IsResponse == false)
                {
                    Logger.Log("Recording Request object with id: {0}", obj.UniqueId);

                    this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                    return;
                }

                IRestrictProcessorObj initialObj = null;
                Logger.Log("Recording response object with id: {0}", obj.UniqueId);
                if (!this.objDict.TryRemove(obj.UniqueId, out initialObj))
                {
                    Logger.Log("Could not find response object with id: {0}", obj.UniqueId);
                    return;
                }

                Logger.Log("Sending response and request object to generate point to send to influx");
                var pointToWrite = GeneratePoint(obj, initialObj);
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

        private Point GeneratePoint(IRestrictProcessorObj currentObj, IRestrictProcessorObj initialObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "auth", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    {"IsSuccessful", currentObj.IsSuccessful },
                    {"InstitutionName", InstitutionInfo.GetInstitutionName(initialObj.InstitutionCode,this.institutionsDict) },
                    {"APICode", currentObj.APICode },
                    //{"IsResponse", currentObj.IsResponse },
                    {"ResponseDescription", currentObj.ResponseDescription },

                },

                Fields = new Dictionary<string, object>()
                {
                    { "ResponseTime", currentObj.TrnxDate.Subtract(initialObj.TrnxDate).TotalMilliseconds },
                    {"InstitutionCode", currentObj.InstitutionCode },
                    { "SuccessCount", currentObj.IsSuccessful == "Successful" ? 1 : 0},
                    { "FailedCount", currentObj.IsSuccessful == "Failed" ? 1 : 0},
                    {"IsResponse", currentObj.IsResponse },
                    {"UniqueId", currentObj.UniqueId },
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


    }
}
