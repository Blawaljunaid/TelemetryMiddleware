using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{


    public class BankOneCacheDuration : BaseProcessor
    {

        Dictionary<string, Institution> institutionsDict = null;


        public BankOneCacheDuration(Loader loader, List<Institution> institutions) : base(loader, "CriticalServicesMonitor", "Fingrid.BankOne.Cache.RequestResponseDuration")
        {
            

            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
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
                float interval;
                float.TryParse(inputParam[1].Trim(), out interval);

                BankOneCacheDurationObj obj = new BankOneCacheDurationObj
                {
                    CacheFunctionType = inputParam[0].Trim(),
                    InstitutionCode = inputParam[2].Trim(),
                    IPAddress = inputParam[1].Trim(),
                    Duration = inputParam[3].Trim(),
                    Interval = interval,

                };
                if (string.IsNullOrEmpty(obj.CacheFunctionType)) obj.CacheFunctionType = "NA";
                if (string.IsNullOrEmpty(obj.InstitutionCode)) obj.InstitutionCode = "NA";
                if (string.IsNullOrEmpty(obj.IPAddress)) obj.IPAddress = "NA";
                if (string.IsNullOrEmpty(obj.Duration)) obj.Duration = "NA";

                Logger.Log("BankOneCacheDuration Object {0},{1},{2},{3},{4} converted.", obj.CacheFunctionType, obj.InstitutionCode, obj.IPAddress, obj.Duration, obj.Interval);


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

                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point GeneratePoint(BankOneCacheDurationObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "BankOneCache",
                Tags = new Dictionary<string, object>()
                {
                    { "CacheFunctionType", currentObj.CacheFunctionType},
                    {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
                    { "institutionCode", currentObj.InstitutionCode },
                    { "IPAddress", currentObj.IPAddress},
                },
                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "institutionCode", currentObj.InstitutionCode },
                    {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
                    { "IPAddress", currentObj.IPAddress},
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


        public class BankOneCacheDurationObj
        {
            public string CacheFunctionType { get; set; }
            public string Duration { get; set; }
            public string InstitutionCode { get; set; }
            public string IPAddress { get; set; }
            public float Interval { get; set; }


        }

    }
}
