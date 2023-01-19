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
    public class BankOneWebAPIMonitor : BaseProcessor
    {

        Dictionary<string, Institution> institutionsDict = null;
        Dictionary<string, string> integrationFIs = null;
        Dictionary<string, string> BanKInTheBoxFIs = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;

        public BankOneWebAPIMonitor(Loader loader, List<Institution> institutions) : base(loader, "CriticalServicesMonitor", "Fingrid.BankOne.WebApi.RequestResponseMonitoring")
        {

            this.objDict = new ConcurrentDictionary<string, BankOneWebAPIMonitorObj>();

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

        ConcurrentDictionary<string, BankOneWebAPIMonitorObj> objDict = null;


        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {


                Logger.Log("");
                Logger.Log(message);
                //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
                message = message.Trim('"', ' ', '\"');
                Logger.Log(message);
                float interval;
                string[] inputParam = message.Split(',');
                float.TryParse(inputParam[1].Trim(), out interval);

                BankOneWebAPIMonitorObj obj = new BankOneWebAPIMonitorObj
                {
                    Methodname = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    Duration = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    ResponseCode = !string.IsNullOrEmpty(inputParam[2]) ? inputParam[2].Trim() : "NA",
                    Institutioncode = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    Interval = interval,
                };


                Logger.Log("BankOneWebAPIMonitor Object {0},{1},{2},{3},{4} converted.", obj.Methodname, obj.Duration, obj.ResponseCode, obj.Institutioncode, obj.Interval);

                Logger.Log("Generate point to write to influx");
                var pointToWrite = GeneratePoint(obj);
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

        private Point GeneratePoint(BankOneWebAPIMonitorObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "BankOneWebAPIMonitorDuration",
                Tags = new Dictionary<string, object>()
                   {

                   {"InstitutionName", GetInstitutionName(currentObj.Institutioncode) },
                   {"Methodname", currentObj.Methodname },
                   {"ResponseCode", currentObj.ResponseCode },

                   },
                Fields = new Dictionary<string, object>()
                   {
                   //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                  
                   { "Duration", currentObj.Duration},
                   { "Interval", currentObj.Interval},
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
        public class BankOneWebAPIMonitorObj
        {
            public string Methodname { get; set; }
            public string Duration { get; set; }
            public string ResponseCode { get; set; }
            public string Institutioncode { get; set; }
            public float Interval { get; set; }


        }

    }
}

