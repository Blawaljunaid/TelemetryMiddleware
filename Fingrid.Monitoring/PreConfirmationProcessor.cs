using InfluxDB.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fingrid.Monitoring.Utility;

namespace Fingrid.Monitoring
{
    public class PreConfirmationProcessor : BaseProcessor
    {
        Dictionary<string, Institution> institutionsDict = null;
        public PreConfirmationProcessor(Loader loader, List<Institution> institutions)
            : base(loader, "CriticalServicesMonitor", "PRECONFIRMATION")
        {
            this.objDict = new ConcurrentDictionary<string, PreConfirmationObj>();

            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
            }
        }
        ConcurrentDictionary<string, PreConfirmationObj> objDict = null;
        

        protected override async void BreakMessageAndFlush(string message)
        {
            try { 
                Logger.Log("");
                Logger.Log(message);
                //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
                message = message.Trim('"', ' ');
                Logger.Log(message);

                string[] inputParam = message.Split(',');
                //MTI, Date, AcquiringInstitutionID,MFBCode,Uniqueidentifier,ResponseCode
                PreConfirmationObj obj = new PreConfirmationObj
                {
                    MTI = inputParam[0].Trim(),
                    Date = DateTime.ParseExact(inputParam[1].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    AcquiringInstitutionID = inputParam[2].Trim(),
                    MFBCode = inputParam[3].Trim(),
                    Uniqueidentifier = inputParam[4].Trim(),
                    ResponseCode = inputParam[5].Trim(),
                    Type = inputParam[6].Trim(),
                    TechnicalSuccess = inputParam[7].Trim(),

                };

                Logger.Log("PreConfirmation Object Uniqueidentifier, Date, MTI, AcquiringInstitutionID, MFBCode, ResponseCode, Type, TechnicalSuccess");
                Logger.Log("PreConfirmation Object {0},{1},{2},{3},{4},{5},{6},{7} converted.", obj.Uniqueidentifier, obj.Date, obj.MTI, obj.AcquiringInstitutionID, obj.MFBCode, obj.ResponseCode, obj.Type, obj.TechnicalSuccess);


                if (obj.MTI == "200")
                {
                    Logger.Log("Recording Request object with id: {0}", obj.Uniqueidentifier);

                    this.objDict.AddOrUpdate(obj.Uniqueidentifier, obj, (val1, val2) => obj);
                    return;
                }

                PreConfirmationObj initialObj = null;
                Logger.Log("Recording response object with id: {0}", obj.Uniqueidentifier);

                if (!this.objDict.TryRemove(obj.Uniqueidentifier, out initialObj))
                {
                    Logger.Log("Could not find response object with id: {0}", obj.Uniqueidentifier);

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

        private Point GeneratePoint(PreConfirmationObj currentObj, PreConfirmationObj initialObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "PRECONFIRMATION", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {

                    {"AcquiringInstitutionID", currentObj.AcquiringInstitutionID },
                    {"MFBCode", currentObj.MFBCode },
                    {"Type", currentObj.Type },
                    {"ResponseCode", currentObj.ResponseCode },
                    {"TechnicalSuccess", currentObj.TechnicalSuccess },
                    {"InstitutionName", GetInstitutionName(currentObj.MFBCode) },

                },

                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "ResponseTime", currentObj.Date.Subtract(initialObj.Date).TotalMilliseconds },
                    { "SuccessCnt", currentObj.TechnicalSuccess == "Successful" ? 1 : 0 },
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }

        private object GetInstitutionName(string institution)
        {
            Institution inst = null;
            if (!this.institutionsDict.TryGetValue(institution.Trim(), out inst))
            {
                inst = InstitutionInfo.GetInstitutionByCode(institution);
            }

            if (inst == null) inst = new Institution { Name = institution, Code = institution };

            return !String.IsNullOrEmpty(inst.Name) ? inst.Name : institution;
        }

        public class PreConfirmationObj
        {
            public string MTI { get; set; }
            public DateTime Date { get; set; }
            public string AcquiringInstitutionID { get; set; }
            public string MFBCode { get; set; }
            public string Uniqueidentifier { get; set; }
            public string ResponseCode { get; set; }
            public string Type { get; set; }
            public string TechnicalSuccess { get; set; }



        }


    }

}
