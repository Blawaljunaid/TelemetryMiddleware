using Fingrid.Monitoring.Utility;
using InfluxDB.Net;
using InfluxDB.Net.Infrastructure.Influx;
using InfluxDB.Net.Models;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class BaseIBWebServerFlow : BaseProcessor
    {
        Dictionary<string, Institution> institutionsDict = null;
        string environment;
        Dictionary<string, string> integrationFIs = null;
        Dictionary<string, string> BanKInTheBoxFIs = null;
        Dictionary<string, string> technicalSuccessItems = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;

        public BaseIBWebServerFlow(Loader loader, string channelName, string environmentName, List<Institution> institutions)
            : base(loader, "IBServerClientFlow", channelName)
        {
            


            this.environment = environmentName;


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

            technicalSuccessItems = new Dictionary<string, string>();
            string concatenatedTechnicalSuccess = ConfigurationManager.AppSettings["ThirdpartyTechnicalSuccessCodes"];
            if (!String.IsNullOrEmpty(concatenatedTechnicalSuccess))
            {
                foreach (string s in concatenatedTechnicalSuccess.Split(','))
                {
                    technicalSuccessItems.Add(s.Trim(), s.Trim());

                }
            }


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
                //MTI, Date, AcquiringInstitutionID,MFBCode,Uniqueidentifier,ResponseCode
                BaseIBWebServerFlowObj obj = new BaseIBWebServerFlowObj
                {
                    UniqueID = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    FlowId = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    FlowName = !string.IsNullOrEmpty(inputParam[2]) ? inputParam[2].Trim() : "NA",
                    Process = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    DeviceIMEI = !string.IsNullOrEmpty(inputParam[6]) ? inputParam[6].Trim() : "NA",
                    CustomerID = !string.IsNullOrEmpty(inputParam[7]) ? inputParam[7].Trim() : "NA",
                    InstitutionCode = !string.IsNullOrEmpty(inputParam[8]) ? inputParam[8].Trim() : "NA",
                    SessionStartTime = DateTime.ParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    SessionEndTime = DateTime.ParseExact(inputParam[5].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    //SessionStartTime = DateTime.Parse(inputParam[4].Trim()),
                    //SessionEndTime = DateTime.Parse(inputParam[5].Trim()),
                    Status = !string.IsNullOrEmpty(inputParam[9]) ? inputParam[9].Trim() : "NA",

                  
                };

                if (testInstitutionCodesToSkip.ContainsKey(obj.InstitutionCode))
                {
                    return;
                }

                Logger.Log("BaseIBWebServerFlow Object {0},{1},{2},{3},{4},{5},{6} converted.", obj.UniqueID, obj.InstitutionCode, obj.CustomerID, obj.FlowId, obj.FlowName, obj.SessionStartTime, obj.SessionEndTime);



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
                Trace.TraceInformation(ex.Message + ex.StackTrace);
                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }
        }

        private Point GeneratePoint(BaseIBWebServerFlowObj currentObj)
        {
            double totalMillisecs = currentObj.SessionEndTime.Subtract(currentObj.SessionStartTime).TotalMilliseconds;
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "BaseIBWebServerFlow",
                Tags = new Dictionary<string, object>()
                   {
                    {"InstitutionGroup", GetGroup(currentObj.InstitutionCode, this.integrationFIs, this.BanKInTheBoxFIs)},
                   {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
                   {"FlowName", currentObj.FlowName },
                   {"Environment" ,  this.environment },
                   {"Status", currentObj.Status },
                   {"Process", currentObj.Process },
                   

                   },
                Fields = new Dictionary<string, object>()
                   {
                   //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                   { "Duration", totalMillisecs},
                   { "CustomerID", currentObj.CustomerID},
                   { "DeviceIMEI", currentObj.DeviceIMEI},

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

        private bool IsSuccessful(string responseCode)
        {
            if (!String.IsNullOrEmpty(responseCode) && this.technicalSuccessItems.ContainsKey(responseCode.Trim()))
            {
                return true;
            }
            return false;
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


        public class BaseIBWebServerFlowObj
        {
            public string UniqueID { get; set; }
            public string FlowId { get; set; }
            public string FlowName { get; set; }
            public string Status { get; set; }
            public string InstitutionCode { get; set; }
            public string Process { get; set; }
            public DateTime SessionStartTime { get; set; }
            public DateTime SessionEndTime { get; set; }
            public string DeviceIMEI { get; set; }
            public string CustomerID { get; set; }

        }


    }
}

