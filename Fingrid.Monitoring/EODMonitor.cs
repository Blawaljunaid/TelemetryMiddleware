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


    public class EODMonitor : BaseProcessor
    {
        Dictionary<string, Institution> institutionsDict = null;
        Dictionary<string, string> BanKInTheBoxFIs = null;
       

        public EODMonitor(Loader loader, List<Institution> institutions) : base(loader, "CriticalServicesMonitor", "Fingrid.BankOne.Corebanking.EOD.Live")
        {

            


            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
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
        

        protected virtual string GetUniqueId(string[] inputParams)
        {
            //bool fromSwitch = Convert.ToBoolean(inputParams[3].Trim());
            //string MTI = inputParams[4].Trim();
            //string uniqueID;
            //if (!fromSwitch && MTI == "200")
            //{
            //    uniqueID = inputParams[12].Trim();//Strip out 200, 210 etc.
            //}
            //else uniqueID = inputParams[14].Trim();

            return $"{inputParams[1].Trim()}:{inputParams[0].Trim()}";
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
                //string Response = "Start";

                EODMonitorObj obj = new EODMonitorObj
                {
                    FinancialDate = inputParam[1].Trim(),
                    institutionCode = inputParam[0].Trim(),
                    Status = inputParam[2].Trim(),
                    Date = DateTime.Now,
                    UniqueId = GetUniqueId(inputParam),
                };
                if (string.IsNullOrEmpty(obj.FinancialDate)) obj.FinancialDate = "NA";
                if (string.IsNullOrEmpty(obj.institutionCode)) obj.institutionCode = "NA";
                if (string.IsNullOrEmpty(obj.Status)) obj.Status = "NA";

                Logger.Log("EODMonitor Object {0},{1},{2},{3},{4} converted", obj.UniqueId, obj.Date, obj.FinancialDate, obj.institutionCode, obj.Status);


                Logger.Log("Generate point to write to influx");
                var pointToWrite = GeneratePoint(obj);
                Logger.Log("Generate point complete");


                Logger.Log("Start writing to influx");
                //Point is then passed into Client.WriteAsync method together with the database name:
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                Logger.Log("DONE writing to influx");
                //var response2 = await influxDbClient.WriteAsync(databaseName, pointsToWrite);

            }
            catch (Exception ex)
            {

                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point GeneratePoint(EODMonitorObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "EODMonitor",
                Tags = new Dictionary<string, object>()
                {
                    { "institutionName", GetInstitutionName(currentObj.institutionCode) },
                    { "institutionCode", currentObj.institutionCode },
                    { "StatusType", currentObj.Status},
                    { "TagStartStatus", currentObj.Status== "Start" ? 1 : 0},
                    { "TagEndStatus", currentObj.Status== "End" ? 1 : 0},
                },
                Fields = new Dictionary<string, object>()
                {
                    { "FinancialDate", currentObj.FinancialDate},
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "Status", currentObj.Status},
                    { "StartStatus", currentObj.Status== "Start" ? 1 : 0},
                    { "EndStatus", currentObj.Status== "End" ? 1 : 0},
                    { "StatusTime", currentObj.Date},
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }

        //private Point GeneratePoints(EODMonitorObj currentObj, EODMonitorObj initialObj)
        //{
        //    var pointsToWrite = new Point()
        //    {
        //        Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
        //        Measurement = "EODMonitor2",
        //        Tags = new Dictionary<string, object>()
        //        {
        //            { "institutionName", GetInstitutionName(currentObj.institutionCode) },
        //            { "institutionCode", currentObj.institutionCode },
                    
        //        },
        //        Fields = new Dictionary<string, object>()
        //        {
        //            { "FinancialDate", currentObj.FinancialDate},
        //            //{ "Duration", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
        //            { "Duration", currentObj.Date.Subtract(initialObj.Date).TotalMilliseconds },
        //            { "StartTime", initialObj.Date},
        //            { "EndTime", currentObj.Date},
        //            {"TotalCnt", 1},
        //        },
        //        Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
        //    };

        //    return pointsToWrite;
        //}


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


        public class EODMonitorObj
        {
            public string FinancialDate { get; set; }
            public DateTime Date { get; set; }
            public string institutionCode { get; set; }
            public string Status { get; set; }
            public string UniqueId { get; set; }

        }

    }
}
