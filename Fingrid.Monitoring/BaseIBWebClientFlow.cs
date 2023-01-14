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
    public class BaseIBWebClientFlow : BaseProcessor
    {
        Dictionary<string, Institution> institutionsDict = null;
        string environment;
        Dictionary<string, string> integrationFIs = null;
        Dictionary<string, string> BanKInTheBoxFIs = null;
        Dictionary<string, string> technicalSuccessItems = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;

        public BaseIBWebClientFlow(Loader loader, string channelName, string environmentName, List<Institution> institutions)
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
                int totalRequestCount, errorMessageResponseCount, noInternetResponseCount, noServerResponseCount, successResponseCount;
                string[] inputParam = message.Split(',');
                int.TryParse(inputParam[8].Trim(), out totalRequestCount);
                int.TryParse(inputParam[11].Trim(), out errorMessageResponseCount);
                int.TryParse(inputParam[10].Trim(), out noInternetResponseCount);
                int.TryParse(inputParam[12].Trim(), out noServerResponseCount);
                int.TryParse(inputParam[9].Trim(), out successResponseCount);
                BaseIBWebClientFlowobj obj = new BaseIBWebClientFlowobj
                {
                    TotalRequestCount = totalRequestCount,
                    ErrorMessageResponseCount = errorMessageResponseCount,
                    NoInternetResponseCount = noInternetResponseCount,
                    NoServerResponseCount = noServerResponseCount,
                    SuccessResponseCount = successResponseCount,
                    UniqueID = inputParam[0].Trim(),
                    FlowId = inputParam[1].Trim(),
                    FlowName = inputParam[2].Trim(),
                    Status = inputParam[7].Trim(),
                    SessionStartTime = DateTime.ParseExact(inputParam[3].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    SessionEndTime = DateTime.ParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    //SessionStartTime = DateTime.Parse(inputParam[3].Trim()),
                    //SessionEndTime = DateTime.Parse(inputParam[4].Trim()),
                    InstitutionCode = inputParam[6].Trim(),
                    CustomerID = inputParam[5].Trim(),
                    

                };
                if (string.IsNullOrEmpty(obj.UniqueID)) obj.UniqueID = "NA";
                if (string.IsNullOrEmpty(obj.FlowId)) obj.FlowId = "NA";
                if (string.IsNullOrEmpty(obj.FlowName)) obj.FlowName = "NA";
                if (string.IsNullOrEmpty(obj.CustomerID)) obj.CustomerID = "NA";
                if (string.IsNullOrEmpty(obj.Status)) obj.Status = "NA";
                if (string.IsNullOrEmpty(obj.InstitutionCode)) obj.InstitutionCode = "NA";

                if (testInstitutionCodesToSkip.ContainsKey(obj.InstitutionCode))
                {
                    return;
                }


                Logger.Log("BaseIBWebClientFlow Object {0},{1},{2},{3},{4},{5},{6} converted.", obj.UniqueID, obj.InstitutionCode, obj.CustomerID, obj.FlowId, obj.FlowName, obj.SessionStartTime, obj.SessionEndTime);


                Logger.Log("Generate point to write to influx");
                var pointToWrite = GeneratePoints(obj);
                Logger.Log("Generate point complete");

                Logger.Log("Start writing to influx");
                //Point is then passed into Client.WriteAsync method together with the database name:
                
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                
                Logger.Log("DONE writing to influx");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message + ex.StackTrace);
                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }
        }

        private Point[] GeneratePoints(BaseIBWebClientFlowobj currentObj)
        {
            Point[] thePoints = new Point[4];
            double totalMillisecs = currentObj.SessionEndTime.Subtract(currentObj.SessionStartTime).TotalMilliseconds;

            Dictionary<string, int> responses = new Dictionary<string, int>();
            responses.Add("ErrorMessageResponseCount", currentObj.ErrorMessageResponseCount);
            responses.Add("NoInternetResponseCount", currentObj.NoInternetResponseCount);
            responses.Add("NoServerResponseCount", currentObj.NoServerResponseCount);
            responses.Add("SuccessResponseCount", currentObj.SuccessResponseCount);

            int count = 0;
            foreach (var response in responses)
            {
                var pointToWrite = new Point()
                {
                    Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                    Measurement = "BaseIBWebClientFlow",
                    Tags = new Dictionary<string, object>()
                    {
                        {"Environment" ,  this.environment },
                        {"InstitutionGroup", GetGroup(currentObj.InstitutionCode, this.integrationFIs, this.BanKInTheBoxFIs)},
                        {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
                        { "Response", response.Key },
                        {"FlowName", currentObj.FlowName },
                        {"Status", currentObj.Status },
                    },
                    Fields = new Dictionary<string, object>()
                    {
                        //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                        { "TotalRequestCount", currentObj.TotalRequestCount },
                        { "ResponseCount", response.Value},
                        { "SessionDuration", totalMillisecs},
                        { "UniqueID", currentObj.UniqueID},
                       {"TotalCnt", 1},
                    },
                    Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
                };
                thePoints[count] = pointToWrite;
                count++;
            }

            return thePoints;
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


        public class BaseIBWebClientFlowobj
        {
            public string UniqueID { get; set; }
            public string FlowId { get; set; }
            public string FlowName { get; set; }
            public string Status { get; set; }
            public string InstitutionCode { get; set; }
            public int TotalRequestCount { get; set; }
            public DateTime SessionStartTime { get; set; }
            public DateTime SessionEndTime { get; set; }
            public int ErrorMessageResponseCount { get; set; }
            public int NoInternetResponseCount { get; set; }
            public int NoServerResponseCount { get; set; }
            public int SuccessResponseCount { get; set; }
            public string CustomerID { get; set; }


        }


    }
}


