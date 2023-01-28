using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;

namespace Fingrid.Monitoring
{
    public class BankoneWebApi : BaseProcessor
    {
        Dictionary<string, Institution> institutionsDict = null;
        Dictionary<string, string> integrationFIs = null;
        Dictionary<string, string> BanKInTheBoxFIs = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;
        ConcurrentDictionary<string, BankoneWebApiObj> objDict = null;

        public BankoneWebApi(Loader loader, List<Institution> institutions)
            : base(loader, "BankoneWebApi", "BankOne.WebApi.RequestResponseMonitoring")
        {
            this.objDict = new ConcurrentDictionary<string, BankoneWebApiObj>();

            this.institutionsDict = new Dictionary<string, Institution>();

            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
            }

            this.integrationFIs = new Dictionary<string, string>();
            string integrationFIsConcatenated = ConfigurationManager.AppSettings["IntegrationFIs"];
            if (!String.IsNullOrEmpty(integrationFIsConcatenated))
            {
                foreach (var s in integrationFIsConcatenated.Split(','))
                {
                    integrationFIs.Add(s.Trim(), s.Trim());
                }
            }

            testInstitutionCodesToSkip = new Dictionary<string, string>();
            string testInstitutionCodes = ConfigurationManager.AppSettings["TestInstitutionCodes"];
            foreach (string s in testInstitutionCodes.Split(','))
            {
                testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            }

            this.BanKInTheBoxFIs = new Dictionary<string, string>();
            string BanKInTheBoxFIsConcatenated = ConfigurationManager.AppSettings["BanKInTheBoxFIs"];
            if (!String.IsNullOrEmpty(BanKInTheBoxFIsConcatenated))
            {
                foreach (var s in BanKInTheBoxFIsConcatenated.Split(','))
                {
                    BanKInTheBoxFIs.Add(s.Trim(), s.Trim());
                }
            }
        }

        public class BankoneWebApiObj
        {
            public string Method { get; set; }
            public string UniqueId { get; set; }
            public bool IsResponse { get; set; }
            public DateTime Time { get; set; }
            public string Status { get; set; }
            public string ErrorMessage { get; set; }
            public string InstitutionCode { get; set; }
            public string Environment { get; set; }
        }

        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {


                Logger.Log("");
                Logger.Log(message);
                // sample message:  "GetAccountByAccountNumber,8d44f45d-d12e-4521-bb12-28e24b8c5ae2,true,20-01-2023 11:38:12:5743448 AM,Failed,No savings/current account was found with the account number: 00510012000003282,100614,dev"
                message = message.Trim('"', ' ');
                Logger.Log(message);

                string[] inputParam = message.Split(',');

                BankoneWebApiObj obj = new BankoneWebApiObj
                {
                    Method = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    UniqueId = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    IsResponse = bool.Parse(inputParam[2].Trim()),
                    Time = DateTime.ParseExact(inputParam[3].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    Status = !string.IsNullOrEmpty(inputParam[4]) ? inputParam[4].Trim() : "NA",
                    ErrorMessage = !string.IsNullOrEmpty(inputParam[5]) ? inputParam[5].Trim() : "NA",
                    InstitutionCode = !string.IsNullOrEmpty(inputParam[6]) ? inputParam[6].Trim() : "NA",
                    Environment = !string.IsNullOrEmpty(inputParam[7]) ? inputParam[7].Trim() : "NA"
                };


                Logger.Log("BankOneWebAPIMonitor Object {0},{1},{2},{3},{4},{5} converted.", obj.UniqueId, obj.Method, obj.IsResponse, obj.Status, obj.Environment, obj.InstitutionCode);

                if (!obj.IsResponse)
                {
                    Console.WriteLine("Recording Request object with id: {0}", obj.UniqueId);
                    this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                    return;
                }

                BankoneWebApiObj initialObj = null;
                Console.WriteLine("Recording response object with id: {0}", obj.UniqueId);
                if (!this.objDict.TryRemove(obj.UniqueId, out initialObj))
                {
                    Console.WriteLine("Could not find response object with id: {0}", obj.UniqueId);
                    return;
                }

                Console.WriteLine("Sending response and request object to generate point to send to influx");
                var pointToWrite = GeneratePoint(obj, initialObj);
                Console.WriteLine("Generate point complete");


                Console.WriteLine("Start writing to influx");
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                Console.WriteLine("DONE writing to influx");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point GeneratePoint(BankoneWebApiObj currentObj, BankoneWebApiObj initialObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx",
                Tags = new Dictionary<string, object>()
                {

                    {"InstitutionCode", currentObj.InstitutionCode },
                    {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
                    {"InstitutionGroup" , GetGroup(currentObj.InstitutionCode, this.integrationFIs, this.BanKInTheBoxFIs) },
                    {"Method", currentObj.Method },
                    {"Status", currentObj.Status },
                    {"ErrorMessage", currentObj.ErrorMessage },
                    {"Environment", currentObj.Environment },

                },
                Fields = new Dictionary<string, object>()
                {                 
                    {"Time", currentObj.Time.Subtract(initialObj.Time).TotalMilliseconds },
                    {"SuccessCnt", currentObj.Status == "Successful" ? 1 : 0 },
                    {"TotalCnt", 1},
                    {"PendingCount", currentObj.Status == "Pending" ? 1 : 0},
                    {"FailedCount", currentObj.Status == "Failed" ? 1 : 0},
                    {"UniqueId", currentObj.UniqueId },
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

        private string GetGroup(string institutionCode, Dictionary<string, string> integrationInstitutions, Dictionary<string, string> BankInTheBoxMFBs)
        {
            if (integrationInstitutions.ContainsKey(institutionCode))
            {
                return "Integration FIs";
            }

            if (BankInTheBoxMFBs.ContainsKey(institutionCode))
            {
                return "BankInTheBox FIs";
            }

            switch (institutionCode)
            {
                case "100040":
                    return "DBN";

                case "100567":
                    return "Sterling";

                case "100592":
                    return "BOI";

                case "100636":
                    return "Access";

                default:
                    return "Others";
            }

        }
    }
}
