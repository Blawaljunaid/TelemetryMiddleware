using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{


    public class BaseMobileServerFlow : BaseProcessor
    {
        string environment;
        Dictionary<string, Institution> institutionsDict = null;
        public BaseMobileServerFlow(Loader loader, List<Institution> institutions, string environmentName, string channelName) :
            base(loader, "InternetBankingTransactions", channelName)
        {
            environment = environmentName;
            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
            }
            Logger.Log("BaseMobileServerFlow");
        }

        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {

                Logger.Log("");
                Logger.Log(message);
                //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
                message = message.Trim('"', ' ', '\"');
                Logger.Log(message);

                string[] inputParam = message.Split(',');

                BaseMobileServerFlowobj obj = new BaseMobileServerFlowobj
                {

                    TransactionId = inputParam[0].Trim(),
                    FlowID = inputParam[2].Trim(),
                    ProcessName = inputParam[4].Trim(),
                    FlowName = inputParam[3].Trim(),
                    ProcessStatus = inputParam[7].Trim(),
                    DateStarted = DateTime.ParseExact(inputParam[5].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    DateEnded = DateTime.ParseExact(inputParam[6].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    InstitutionCode = inputParam[1].Trim(),

                };
                if (string.IsNullOrEmpty(obj.TransactionId)) obj.TransactionId = "NA";
                if (string.IsNullOrEmpty(obj.InstitutionCode)) obj.InstitutionCode = "NA";

                Logger.Log("BaseMobileServerFlow Object {0},{1},{2},{3},{4},{5},{6},{7} converted", obj.TransactionId, obj.InstitutionCode, obj.FlowID, obj.ProcessName, obj.FlowName, obj.ProcessStatus, obj.DateStarted, obj.DateEnded);

                //var pointToWrite = GeneratePoint(obj);

                //Logger.Log("about to log pointtowrite" + Newtonsoft.Json.JsonConvert.SerializeObject(pointToWrite));
                ////Point is then passed into Client.WriteAsync method together with the database name:
                //var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
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

                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point GeneratePoint(BaseMobileServerFlowobj currentObj)
        {

            double totalMillisecs = currentObj.DateEnded.Subtract(currentObj.DateStarted).TotalMilliseconds;

            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "MobileServerProcessFlow",
                Tags = new Dictionary<string, object>()
                                {
                                    {"InstitutionGroup", GetGroup(currentObj.InstitutionCode)},
                                    {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },

                                    { "ProcessName", currentObj.ProcessName},
                                    { "FlowName", currentObj.FlowName},
                                    { "ProcessStatus", currentObj.ProcessStatus},
                                    {"Environment" ,  this.environment },

                                },
                Fields = new Dictionary<string, object>()
                                {
                                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },

                                    { "SessionDuration", totalMillisecs},
                                    { "UniqueID", currentObj.TransactionId},
                                    { "FlowID", currentObj.FlowID},
                                    { "TotalCnt", 1},
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

        private object GetInstitutionName(string institution)
        {
            Institution inst = null;
            if (!this.institutionsDict.TryGetValue(institution.Trim(), out inst))
            {
                inst = InstitutionInfo.GetInstitutionByCode(institution);
            }

            if (inst == null) inst = new Institution { Name = institution, Code = institution };

            if (string.IsNullOrEmpty(institution)) institution = "N/A";
            return !String.IsNullOrEmpty(inst.Name) ? inst.Name : "others";
        }


        public class BaseMobileServerFlowobj
        {
            public string TransactionId { get; set; }
            public string FlowID { get; set; }
            public string ProcessName { get; set; }
            public string FlowName { get; set; }
            public string ProcessStatus { get; set; }

            public string InstitutionCode { get; set; }


            public DateTime DateStarted { get; set; }
            public DateTime DateEnded { get; set; }




        }

    }


    /**
     * GABE commented out because it doesnt seem to be the correct code for this class
     * Number of values in the message is greater than the expected value
     */
    //public class BaseMobileServerFlow: BaseProcessor
    //{
    //    string environment;
    //    Dictionary<string, Institution> institutionsDict = null;
    //    public BaseMobileServerFlow(Loader loader, List<Institution> institutions, string environmentName, string channelName) : 
    //        base(loader, "InternetBankingTransactions", channelName)
    //    {

    //        this.institutionsDict = new Dictionary<string, Institution>();
    //        foreach (var institution in institutions)
    //        {
    //            if (!this.institutionsDict.ContainsKey(institution.Code))
    //                this.institutionsDict.Add(institution.Code, institution);
    //        }
    //        Logger.Log("BaseMobileServerFlow");
    //    }

    //    protected override async void BreakMessageAndFlush(string message)
    //    {
    //        try
    //        {


    //            Logger.Log("");
    //            Logger.Log(message);
    //            //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
    //            message = message.Trim('"', ' ');
    //            Logger.Log(message);
    //            float ErrorResponsCount, NoInternetCount, SuccessCount, RequestCount, NoResponseCount, TotalMemorySpace, MemorySpaceRemaining, TotalRAMSize, PercentageRAMRemaining;
    //            string[] inputParam = message.Split(',');
    //            float.TryParse(inputParam[13].Trim(), out ErrorResponsCount);
    //            float.TryParse(inputParam[12].Trim(), out NoInternetCount);
    //            float.TryParse(inputParam[11].Trim(), out SuccessCount);
    //            float.TryParse(inputParam[10].Trim(), out RequestCount);
    //            float.TryParse(inputParam[14].Trim(), out NoResponseCount);
    //            float.TryParse(inputParam[17].Trim(), out TotalMemorySpace);
    //            float.TryParse(inputParam[18].Trim(), out MemorySpaceRemaining);
    //            float.TryParse(inputParam[15].Trim(), out TotalRAMSize);
    //            float.TryParse(inputParam[16].Trim(), out PercentageRAMRemaining);

    //            BaseMobileServerFlowobj obj = new BaseMobileServerFlowobj
    //            {
    //                ErrorResponsCount = ErrorResponsCount,
    //                NoInternetCount = NoInternetCount,
    //                SuccessCount = SuccessCount,
    //                RequestCount = RequestCount,
    //                NoResponseCount = NoResponseCount,
    //                TotalMemorySpace = TotalMemorySpace,
    //                MemorySpaceRemaining = MemorySpaceRemaining,
    //                TotalRAMSize = TotalRAMSize,
    //                PercentageRAMRemaining = PercentageRAMRemaining,
    //                TransactionId = inputParam[0].Trim(),
    //                FlowID = inputParam[1].Trim(),
    //                ProcessName = inputParam[3].Trim(),
    //                FlowName = inputParam[2].Trim(),
    //                ProcessStatus = inputParam[9].Trim(),
    //                DeviceIMEI = inputParam[6].Trim(),
    //                CustomerID = inputParam[7].Trim(),
    //                DateStarted = DateTime.ParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
    //                DateEnded = DateTime.ParseExact(inputParam[5].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
    //                InstitutionCode = inputParam[2].Trim(),
    //                BankOneInstitutionCode = inputParam[8].Trim(),



    //            };
    //            if (string.IsNullOrEmpty(obj.TransactionId)) obj.TransactionId = "NA";
    //            if (string.IsNullOrEmpty(obj.InstitutionCode)) obj.InstitutionCode = "NA";
    //            if (string.IsNullOrEmpty(obj.FlowID)) obj.FlowID = "NA";
    //            if (string.IsNullOrEmpty(obj.FlowName)) obj.FlowName = "NA";
    //            if (string.IsNullOrEmpty(obj.ProcessName)) obj.ProcessName = "NA";
    //            if (string.IsNullOrEmpty(obj.ProcessStatus)) obj.ProcessStatus = "NA";
    //            if (string.IsNullOrEmpty(obj.DeviceIMEI)) obj.DeviceIMEI = "NA";
    //            if (string.IsNullOrEmpty(obj.CustomerID)) obj.CustomerID = "NA";
    //            if (string.IsNullOrEmpty(obj.BankOneInstitutionCode)) obj.BankOneInstitutionCode = "NA";

    //            //Logger.Log("BaseMobileServerFlow Object {0},{1},{2},{3},{4},{5} converted.", obj.TransactionId, obj.InstitutionCode, obj.ProcessStatus, obj.DeviceIMEI, obj.CustomerID, obj.FlowID);

    //            Logger.Log("Generate point to write to influx");
    //            var pointToWrite = GeneratePoints(obj);
    //            Logger.Log("Generate point complete");

    //            Logger.Log("Start writing to influx");
    //            //Point is then passed into Client.WriteAsync method together with the database name:
    //            var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);

    //            Logger.Log("DONE writing to influx");

    //        }
    //        catch (Exception ex)
    //        {

    //            Logger.Log(ex.Message);
    //            Logger.Log(ex.InnerException?.Message);
    //            Logger.Log(ex.StackTrace);
    //        }

    //    }

    //    private Point[] GeneratePoints(BaseMobileServerFlowobj currentObj)
    //    {
    //        Point[] thePoints = new Point[4];
    //        double totalMillisecs = currentObj.DateEnded.Subtract(currentObj.DateStarted).TotalMilliseconds;

    //        Dictionary<string, float> responses = new Dictionary<string, float>();
    //        responses.Add("ErrorResponseCount", currentObj.ErrorResponsCount);
    //        responses.Add("NoInternetResponseCount", currentObj.NoInternetCount);
    //        responses.Add("NoServerResponseCount", currentObj.NoResponseCount);
    //        responses.Add("SuccessResponseCount", currentObj.SuccessCount);

    //        int count = 0;
    //        foreach (var response in responses)
    //        {
    //            var pointToWrite = new Point()
    //            {
    //                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
    //                Measurement = "MobileServerTrx",
    //                Tags = new Dictionary<string, object>()
    //                {
    //                    {"InstitutionCode", GetGroup(currentObj.InstitutionCode)},
    //                    {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
    //                    { "Response", response.Key },
    //                    { "ProcessName", currentObj.ProcessName},
    //                    { "FlowName", currentObj.FlowName},
    //                    { "ProcessStatus", currentObj.ProcessStatus},
    //                    {"environment" ,  this.environment },

    //                },
    //                Fields = new Dictionary<string, object>()
    //                {
    //                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
    //                    { "TotalRequestCount", currentObj.RequestCount },
    //                    { "TotalMemorySpace", currentObj.TotalMemorySpace },
    //                    { "MemorySpaceRemaining", currentObj.MemorySpaceRemaining},
    //                    { "TotalRAMSize", currentObj.TotalRAMSize},
    //                    { "PercentageRAMRemaining", currentObj.PercentageRAMRemaining},
    //                    { "ResponseCount", response.Value},
    //                    { "SessionDuration", totalMillisecs},
    //                    { "UniqueID", currentObj.TransactionId},
    //                    { "FlowID", currentObj.FlowID},
    //                    { "DeviceIMEI", currentObj.DeviceIMEI},
    //                    { "CustomerID", currentObj.CustomerID},
    //                   {"TotalCnt", 1},
    //                },
    //                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
    //            };
    //            thePoints[count] = pointToWrite;
    //            count++;
    //        }

    //        return thePoints;
    //    }

    //    private string GetGroup(string institutionCode)
    //    {


    //        switch (institutionCode)
    //        {
    //            case "100040":
    //                return "DBN";

    //            case "100567":
    //                return "Sterling";

    //            case "100592":
    //                return "BOI";

    //            case "100636":
    //                return "Access";

    //            default:
    //                return "Others";
    //        }

    //    }

    //    private object GetInstitutionName(string institution)
    //    {
    //        Institution inst = null;
    //        if (!this.institutionsDict.TryGetValue(institution.Trim(), out inst))
    //        {
    //            inst = InstitutionInfo.GetInstitutionByCode(institution);
    //        }

    //        if (inst == null) inst = new Institution { Name = institution, Code = institution };

    //        if (string.IsNullOrEmpty(institution)) institution = "N/A";
    //        return !String.IsNullOrEmpty(inst.Name) ? inst.Name : "others";
    //    }


    //    public class BaseMobileServerFlowobj
    //    {
    //        public string TransactionId { get; set; }
    //        public string FlowID { get; set; }
    //        public string ProcessName { get; set; }
    //        public string FlowName { get; set; }
    //        public string ProcessStatus { get; set; }
    //        public string DeviceIMEI { get; set; }
    //        public string InstitutionCode { get; set; }
    //        public string BankOneInstitutionCode { get; set; }
    //        public string CustomerID { get; set; }
    //        public DateTime DateStarted { get; set; }
    //        public DateTime DateEnded { get; set; }
    //        public float ErrorResponsCount { get; set; }
    //        public float NoInternetCount { get; set; }
    //        public float SuccessCount { get; set; }
    //        public float RequestCount { get; set; }
    //        public float NoResponseCount { get; set; }
    //        public float TotalMemorySpace { get; set; }
    //        public float MemorySpaceRemaining { get; set; }
    //        public float TotalRAMSize { get; set; }
    //        public float PercentageRAMRemaining { get; set; }



    //    }
    //}
}
