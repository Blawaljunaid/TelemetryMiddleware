using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{

    public class BaseMobileTracker : BaseProcessor
    {
        public BaseMobileTracker(Loader loader, string dbname, string channel) : base(loader, dbname, channel)
        {
            Logger.Log("BaseMobileTracker");
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
                int.TryParse(inputParam[4].Trim(), out totalRequestCount);
                int.TryParse(inputParam[5].Trim(), out errorMessageResponseCount);
                int.TryParse(inputParam[6].Trim(), out noInternetResponseCount);
                int.TryParse(inputParam[7].Trim(), out noServerResponseCount);
                int.TryParse(inputParam[9].Trim(), out successResponseCount);
                BaseMobileTrackerobj obj = new BaseMobileTrackerobj
                {
                    TotalRequestCount = totalRequestCount,
                    ErrorMessageResponseCount = errorMessageResponseCount,
                    NoInternetResponseCount = noInternetResponseCount,
                    NoServerResponseCount = noServerResponseCount,
                    SuccessResponseCount = successResponseCount,
                    UniqueID = inputParam[0].Trim(),
                    DateReceived = DateTime.ParseExact(inputParam[8].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    SessionStartTime = DateTime.ParseExact(inputParam[2].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    SessionEndTime = DateTime.ParseExact(inputParam[3].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    InstitutionCode = inputParam[1].Trim(),
                    AppName = inputParam[10].Trim(),

                };
                if (string.IsNullOrEmpty(obj.UniqueID)) obj.UniqueID = "NA";
                if (string.IsNullOrEmpty(obj.InstitutionCode)) obj.InstitutionCode = "NA";
                Logger.Log("BaseMobileTracker Object UniqueID, InstitutionCode, DateReceived, SessionStartTime, SessionEndTime");
                Logger.Log("BaseMobileTracker Object {0},{1},{2},{3},{4} converted", obj.UniqueID, obj.InstitutionCode, obj.DateReceived, obj.SessionStartTime, obj.SessionEndTime);

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

                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point[] GeneratePoints(BaseMobileTrackerobj currentObj)
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
                    Measurement = "Trx",
                    Tags = new Dictionary<string, object>()
                    {
                        {"InstitutionCode", GetGroup(currentObj.InstitutionCode)},
                        { "Response", response.Key },
                        {"AppName", currentObj.AppName },
                    },
                    Fields = new Dictionary<string, object>()
                    {
                        //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                        { "TotalRequestCount", currentObj.TotalRequestCount },
                        //{ "ErrorMessageResponseCount", currentObj.ErrorMessageResponseCount },
                        //{ "NoInternetResponseCount", currentObj.NoInternetResponseCount},
                        //{ "NoServerResponseCount", currentObj.NoServerResponseCount},
                        //{ "SuccessResponseCount", currentObj.SuccessResponseCount},
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


        public class BaseMobileTrackerobj
        {
            public string UniqueID { get; set; }
            public DateTime DateReceived { get; set; }
            public string InstitutionCode { get; set; }
            public int TotalRequestCount { get; set; }
            public DateTime SessionStartTime { get; set; }
            public DateTime SessionEndTime { get; set; }
            public int ErrorMessageResponseCount { get; set; }
            public int NoInternetResponseCount { get; set; }
            public int NoServerResponseCount { get; set; }
            public int SuccessResponseCount { get; set; }
            public string AppName { get; set; }


        }

    }
}
