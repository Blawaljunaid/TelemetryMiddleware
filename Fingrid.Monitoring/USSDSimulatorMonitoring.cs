using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Fingrid.Monitoring
{


    public class USSDSimulatorMonitoring : BaseProcessor
    {
        public USSDSimulatorMonitoring(Loader loader) : base(loader, "USSDSimulatorTracker", "CreditClub.USSDTracker")
        {

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

                USSDSimulatorMonitoringObj obj = new USSDSimulatorMonitoringObj
                {
                    NetworkOperator = inputParam[6].Trim(),
                    Environment = inputParam[7].Trim(),
                    IsSuccess = Convert.ToBoolean(inputParam[4].Trim()),
                    SessionID = inputParam[0].Trim(),
                    SessionStartTime = DateTime.ParseExact(inputParam[2].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    SessionEndTime = DateTime.ParseExact(inputParam[3].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    InstitutionCode = inputParam[1].Trim(),
                    
                };
                if (string.IsNullOrEmpty(obj.SessionID)) obj.SessionID = "NA";
                if (string.IsNullOrEmpty(obj.InstitutionCode)) obj.InstitutionCode = "NA";

                Logger.Log("USSDSimulatorMonitoring Object NetworkOperator, Environment, IsSuccess, SessionID, SessionStartTime, SessionEndTime, InstitutionCode");
                Logger.Log("USSDSimulatorMonitoring Object {0},{1},{2},{3},{4},{5},{6} converted.", obj.NetworkOperator, obj.Environment, obj.IsSuccess, obj.SessionID, obj.SessionStartTime, obj.SessionEndTime, obj.InstitutionCode);


                Logger.Log("Generate point to write to influx");
                var pointToWrite = GeneratePoints(obj);
                Logger.Log("Generate point complete");

                Logger.Log("Start wriiting to influx");
                //Point is then passed into Client.WriteAsync method together with the database name:
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                Logger.Log("DONE wriiting to influx");

            }
            catch (Exception ex)
            {

                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        //private Point[] GeneratePoints(USSDSimulatorMonitoringObj currentObj)
        //{
        //    double totalMillisecs = currentObj.SessionEndTime.Subtract(currentObj.SessionStartTime).TotalMilliseconds;

        //    Dictionary<string, int> responses = new Dictionary<string, int>();
            
        //        var pointToWrite = new Point()
        //        {
        //            Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
        //            Measurement = "Trx",
        //            Tags = new Dictionary<string, object>()
        //            {
                        
                        
        //            },
        //            Fields = new Dictionary<string, object>()
        //            {
        //                //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
        //                { "TotalRequestCount", currentObj.TotalRequestCount },
        //                //{ "ErrorMessageResponseCount", currentObj.ErrorMessageResponseCount },
        //                //{ "NoInternetResponseCount", currentObj.NoInternetResponseCount},
        //                //{ "NoServerResponseCount", currentObj.NoServerResponseCount},
        //                //{ "SuccessResponseCount", currentObj.SuccessResponseCount},
        //                { "ResponseCount", response.Value},
        //                { "SessionDuration", totalMillisecs},
        //                { "SessionID", currentObj.SessionID},
        //               {"TotalCnt", 1},
        //            },
        //            Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
        //        };
        //    return pointToWrite;
        //}
        private Point GeneratePoints(USSDSimulatorMonitoringObj currentObj)
        {
            double totalMillisecs = currentObj.SessionEndTime.Subtract(currentObj.SessionStartTime).TotalMilliseconds;
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx",
                Tags = new Dictionary<string, object>()
                {

                    {"Environment", currentObj.Environment },
                    {"InstitutionCode", GetGroup(currentObj.InstitutionCode)},
                    {"TechnicalSuccess", currentObj.IsSuccess ? "Successful" : "Failed" },


                },


                Fields = new Dictionary<string, object>()
                {
                        { "SuccessCnt", currentObj.IsSuccess ? 1 : 0 },
                        { "SessionDuration", totalMillisecs},
                        { "SessionID", currentObj.SessionID},
                        {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }

        private string GetGroup(string institutionCode)
        {


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


        public class USSDSimulatorMonitoringObj
        {
            public string SessionID { get; set; }
            public string InstitutionCode { get; set; }
            public DateTime SessionStartTime { get; set; }
            public DateTime SessionEndTime { get; set; }
            public string NetworkOperator { get; set; }
            public bool IsSuccess { get; set; }
            public string Environment { get; set; }
            
        }

    }
}
