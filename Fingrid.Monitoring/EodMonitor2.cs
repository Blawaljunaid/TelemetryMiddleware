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
    public class EodMonitor2 : BaseProcessor
    {
        Dictionary<string, Institution> institutionsDict = null;
        Dictionary<string, string> BanKInTheBoxFIs = null;
        long ExceedsThresholdMin = 0;


        public EodMonitor2(Loader loader, List<Institution> institutions) : base(loader, "CriticalServicesMonitor", "Fingrid.BankOne.Corebanking.EOD.Live")
           
        {
            this.objDict = new ConcurrentDictionary<string, EODMonitor2Obj>();


            ExceedsThresholdMin = Convert.ToInt64(ConfigurationManager.AppSettings["ExceedsThresholdMin"]);


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
        ConcurrentDictionary<string, EODMonitor2Obj> objDict = null;
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


        private  async Task FlushThreshold()
        {
            var keysToRemove = new List<string>();
            foreach(var defaultobj in objDict.Where(x => DateTime.Now >= x.Value.Date.AddMinutes(ExceedsThresholdMin)))
            {
                EODMonitor2Obj obj = new EODMonitor2Obj
                {
                    FinancialDate = defaultobj.Value.FinancialDate,
                    institutionCode = defaultobj.Value.institutionCode,
                    Status = "End",
                    Date = DateTime.Now,
                    UniqueId = defaultobj.Value.UniqueId,
                    IsExceedsThreshold = true
                };
                keysToRemove.Add(defaultobj.Key);
                //var pointToWrite = GeneratePoint(obj, defaultobj.Value);
                ////Point is then passed into Client.WriteAsync method together with the database name:

                //var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);

                Logger.Log("Sending EOD Start and End object to generate point to send to influx");
                var pointToWrite = GeneratePoint(obj, defaultobj.Value);
                Logger.Log("Generate point complete");

                Logger.Log("Start writing to influx");
                //Point is then passed into Client.WriteAsync method together with the database name:
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                Logger.Log("DONE writing to influx");
            }

            foreach(var key in keysToRemove)
            {
                objDict.TryRemove(key, out EODMonitor2Obj flush);
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
                string Response = "start";
                string[] inputParam = message.Split(',');
                //MTI, Date, AcquiringInstitutionID,MFBCode,Uniqueidentifier,ResponseCode
                EODMonitor2Obj obj = new EODMonitor2Obj
                {
                    FinancialDate = inputParam[1].Trim(),
                    institutionCode = inputParam[0].Trim(),
                    Status = inputParam[2].Trim().ToLower(),
                    Date = DateTime.Now,
                    UniqueId = GetUniqueId(inputParam)


                };



                if (string.IsNullOrEmpty(obj.FinancialDate)) obj.FinancialDate = "NA";
                if (string.IsNullOrEmpty(obj.institutionCode)) obj.institutionCode = "NA";
                if (string.IsNullOrEmpty(obj.Status)) obj.Status = "NA";

                Logger.Log("EODMonitor2 Object {0},{1},{2},{3},{4} converted", obj.UniqueId, obj.Date, obj.FinancialDate, obj.institutionCode, obj.Status);

                if (obj.Status == Response)
                {
                    Logger.Log("Recording EOD Start object with id: {0}", obj.UniqueId);
                    this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                    return;
                }
                
                await FlushThreshold();

                // Gabe commented out because everything below is already in the FlushThreshold() function above
                //EODMonitor2Obj initialObj = null;
                //Logger.Log("Recording EOD End object with id: {0}", obj.UniqueId);
                //if (!this.objDict.TryRemove(obj.UniqueId, out initialObj))
                //{
                //    Logger.Log("Could not find EOD End object with id: {0}", obj.UniqueId);
                //    return;
                //}



                //Logger.Log("Sending response and request object to generate point to send to influx");
                //var pointToWrite = GeneratePoint(obj, initialObj);
                //Logger.Log("Generate point complete");

                //Logger.Log("Start writing to influx");
                ////Point is then passed into Client.WriteAsync method together with the database name:
                //var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                //Logger.Log("DONE writing to influx");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message + ex.StackTrace);
            }
        }

        private Point GeneratePoint(EODMonitor2Obj currentObj, EODMonitor2Obj initialObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "EODMonitor2",
                Tags = new Dictionary<string, object>()
                {
                    { "institutionName", GetInstitutionName(currentObj.institutionCode) },
                    { "institutionCode", currentObj.institutionCode },
                    { "IsExceedsThreshold", currentObj.IsExceedsThreshold },

                },
                Fields = new Dictionary<string, object>()
                {
                    { "FinancialDate", currentObj.FinancialDate},
                    //{ "Duration", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "Duration", currentObj.Date.Subtract(initialObj.Date).TotalMilliseconds },
                    { "StartTime", initialObj.Date},
                    { "EndTime", currentObj.Date},
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


        public class EODMonitor2Obj
        {
            public string FinancialDate { get; set; }
            public DateTime Date { get; set; }
            public string institutionCode { get; set; }
            public string Status { get; set; }
            public string UniqueId { get; set; }
            public bool IsExceedsThreshold { get; set; } = false;

        }


    }
}
