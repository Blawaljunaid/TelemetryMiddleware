using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{

    public class InternetBankingClientMobileTracker : BaseProcessor
    {
        string Environment;
        Dictionary<string, Institution> institutionsDict = null;
        public InternetBankingClientMobileTracker(Loader loader, List<Institution> institutions, string environmentName, string channelName) :
            base(loader, "InternetBankingTransactions", channelName)
        {
            Environment = environmentName;
            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
            }
            Logger.Log("BaseClientMobileTracker");
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
                float ErrorResponsCount, NoInternetCount, SuccessCount, RequestCount, NoResponseCount, TotalMemorySpace, MemorySpaceRemaining, TotalRAMSize, PercentageRAMRemaining;
                string[] inputParam = message.Split(',');
                float.TryParse(inputParam[12].Trim(), out ErrorResponsCount);
                float.TryParse(inputParam[11].Trim(), out NoInternetCount);
                float.TryParse(inputParam[10].Trim(), out SuccessCount);
                float.TryParse(inputParam[9].Trim(), out RequestCount);
                float.TryParse(inputParam[13].Trim(), out NoResponseCount);
                float.TryParse(inputParam[16].Trim(), out TotalMemorySpace);
                float.TryParse(inputParam[17].Trim(), out MemorySpaceRemaining);
                float.TryParse(inputParam[14].Trim(), out TotalRAMSize);
                float.TryParse(inputParam[15].Trim(), out PercentageRAMRemaining);

                InternetBankingClientMobileTrackerobj obj = new InternetBankingClientMobileTrackerobj
                {
                    ErrorResponsCount = ErrorResponsCount,
                    NoInternetCount = NoInternetCount,
                    SuccessCount = SuccessCount,
                    RequestCount = RequestCount,
                    NoResponseCount = NoResponseCount,
                    TotalMemorySpace = TotalMemorySpace,
                    MemorySpaceRemaining = MemorySpaceRemaining,
                    TotalRAMSize = TotalRAMSize,
                    PercentageRAMRemaining = PercentageRAMRemaining,
                    TransactionId = inputParam[0].Trim(),
                    FlowID = inputParam[1].Trim(),
                    ProcessName = inputParam[2].Trim(),
                    ProcessStatus = inputParam[8].Trim(),
                    DeviceIMEI = inputParam[5].Trim(),
                    CustomerID = inputParam[6].Trim(),
                    DateStarted = DateTime.ParseExact(inputParam[3].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    DateEnded = DateTime.ParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    InstitutionCode = inputParam[7].Trim(),




                };

                if (string.IsNullOrEmpty(obj.TransactionId)) obj.TransactionId = "NA";
                if (string.IsNullOrEmpty(obj.InstitutionCode)) obj.InstitutionCode = "NA";
                if (string.IsNullOrEmpty(obj.FlowID)) obj.FlowID = "NA";
                if (string.IsNullOrEmpty(obj.ProcessStatus)) obj.ProcessStatus = "NA";
                if (string.IsNullOrEmpty(obj.DeviceIMEI)) obj.DeviceIMEI = "NA";
                if (string.IsNullOrEmpty(obj.CustomerID)) obj.CustomerID = "NA";
                if (string.IsNullOrEmpty(obj.BankOneInstitutionCode)) obj.BankOneInstitutionCode = "NA";

                Logger.Log("InternetBankingClientMobileTracker Object {0},{1},{2},{3},{4},{5} converted.", obj.TransactionId, obj.InstitutionCode, obj.ProcessStatus, obj.DeviceIMEI, obj.CustomerID, obj.FlowID);

                Logger.Log("Generate point to write to influx");
                var pointToWrite = GeneratePoints(obj);
                Logger.Log("Generate point complete");

                Logger.Log("Start writing to influx");
                //Point is then passed into Client.WriteAsync method together with the database name:
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                Logger.Log("DONE writing to influx");



                //var pointToWrite = GeneratePoints(obj);
                //Logger.Log("about to log pointtowrite" + Newtonsoft.Json.JsonConvert.SerializeObject(pointToWrite));
                ////Point is then passed into Client.WriteAsync method together with the database name:

                //var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
            }
            catch (Exception ex)
            {
                //Trace.TraceInformation(ex.Message + ex.StackTrace);
                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }
        }

        private Point[] GeneratePoints(InternetBankingClientMobileTrackerobj currentObj)
        {
            Point[] thePoints = new Point[4];
            double totalMillisecs = currentObj.DateEnded.Subtract(currentObj.DateStarted).TotalMilliseconds;
            Dictionary<string, float> responses = new Dictionary<string, float>();
            responses.Add("ErrorResponseCount", currentObj.ErrorResponsCount);
            responses.Add("NoInternetResponseCount", currentObj.NoInternetCount);
            responses.Add("NoServerResponseCount", currentObj.NoResponseCount);
            responses.Add("SuccessResponseCount", currentObj.SuccessCount);

            int count = 0;
            foreach (var response in responses)
            {
                var pointToWrite = new Point()
                {
                    Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                    Measurement = "MobileClientTrxGabetest",
                    Tags = new Dictionary<string, object>()
                                    {
                                        {"InstitutionCode", GetGroup(currentObj.InstitutionCode)},
                                        {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
                                        { "Response", response.Key },
                                        { "ProcessName", currentObj.ProcessName},
                                        { "ProcessStatus", currentObj.ProcessStatus},
                                        {"Environment" ,  this.Environment },

                                    },
                    Fields = new Dictionary<string, object>()
                                    {
                                        //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                                        { "TotalRequestCount", currentObj.RequestCount },
                                        { "TotalMemorySpace", currentObj.TotalMemorySpace },
                                        { "MemorySpaceRemaining", currentObj.MemorySpaceRemaining},
                                        { "TotalRAMSize", currentObj.TotalRAMSize},
                                        { "PercentageRAMRemaining", currentObj.PercentageRAMRemaining},
                                        { "ResponseCount", response.Value},
                                        { "SessionDuration", totalMillisecs},
                                        { "UniqueID", currentObj.TransactionId},
                                        { "FlowID", currentObj.FlowID},
                                        { "DeviceIMEI", currentObj.DeviceIMEI},
                                        { "CustomerID", currentObj.CustomerID},
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



        public class InternetBankingClientMobileTrackerobj
        {
            public string TransactionId { get; set; }
            public string FlowID { get; set; }
            public string ProcessName { get; set; }
            public string ProcessStatus { get; set; }
            public string DeviceIMEI { get; set; }
            public string InstitutionCode { get; set; }
            public string BankOneInstitutionCode { get; set; }
            public string CustomerID { get; set; }
            public DateTime DateStarted { get; set; }
            public DateTime DateEnded { get; set; }
            public float ErrorResponsCount { get; set; }
            public float NoInternetCount { get; set; }
            public float SuccessCount { get; set; }
            public float RequestCount { get; set; }
            public float NoResponseCount { get; set; }
            public float TotalMemorySpace { get; set; }
            public float MemorySpaceRemaining { get; set; }
            public float TotalRAMSize { get; set; }
            public float PercentageRAMRemaining { get; set; }



        }


    }




/**
 * Gabe commented this because it wasnt working ooo 
 * 
 */

    //public class InternetBankingClientMobileTracker : BaseProcessor
    //{
    //    string environment;
    //    Dictionary<string, Institution> institutionsDict = null;
    //    public InternetBankingClientMobileTracker(Loader loader, List<Institution> institutions, string environmentName, string channelName) :
    //        base(loader, "InternetBankingTransactions", channelName)
    //    {

    //        this.institutionsDict = new Dictionary<string, Institution>();
    //        foreach (var institution in institutions)
    //        {
    //            if (!this.institutionsDict.ContainsKey(institution.Code))
    //                this.institutionsDict.Add(institution.Code, institution);
    //        }
    //        Logger.Log("BaseClientMobileTracker");
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
    //            float.TryParse(inputParam[12].Trim(), out ErrorResponsCount);
    //            float.TryParse(inputParam[11].Trim(), out NoInternetCount);
    //            float.TryParse(inputParam[10].Trim(), out SuccessCount);
    //            float.TryParse(inputParam[9].Trim(), out RequestCount);
    //            float.TryParse(inputParam[13].Trim(), out NoResponseCount);
    //            float.TryParse(inputParam[16].Trim(), out TotalMemorySpace);
    //            float.TryParse(inputParam[17].Trim(), out MemorySpaceRemaining);
    //            float.TryParse(inputParam[14].Trim(), out TotalRAMSize);
    //            float.TryParse(inputParam[15].Trim(), out PercentageRAMRemaining);

    //            InternetBankingClientMobileTrackerobj obj = new InternetBankingClientMobileTrackerobj
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
    //                ProcessName = inputParam[2].Trim(),
    //                ProcessStatus = inputParam[8].Trim(),
    //                DeviceIMEI = inputParam[5].Trim(),
    //                CustomerID = inputParam[6].Trim(),
    //                DateStarted = DateTime.ParseExact(inputParam[3].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
    //                DateEnded = DateTime.ParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
    //                InstitutionCode = inputParam[7].Trim(),
    //                BankOneInstitutionCode = inputParam[7].Trim(),



    //            };
    //            if (string.IsNullOrEmpty(obj.TransactionId)) obj.TransactionId = "NA";
    //            if (string.IsNullOrEmpty(obj.InstitutionCode)) obj.InstitutionCode = "NA";
    //            if (string.IsNullOrEmpty(obj.FlowID)) obj.FlowID = "NA";
    //            if (string.IsNullOrEmpty(obj.ProcessStatus)) obj.ProcessStatus = "NA";
    //            if (string.IsNullOrEmpty(obj.DeviceIMEI)) obj.DeviceIMEI = "NA";
    //            if (string.IsNullOrEmpty(obj.CustomerID)) obj.CustomerID = "NA";
    //            if (string.IsNullOrEmpty(obj.BankOneInstitutionCode)) obj.BankOneInstitutionCode = "NA";

    //            Logger.Log("BaseClientMobileTracker Object {0},{1},{2},{3},{4},{5} converted.", obj.TransactionId, obj.InstitutionCode, obj.ProcessStatus, obj.DeviceIMEI, obj.CustomerID, obj.FlowID);

    //            Logger.Log("Generate point to write to influx");
    //            var pointToWrite = GeneratePoints(obj);
    //            Logger.Log("Generate point complete");

    //            Logger.Log("Start writing to influx");
    //            //Point is then passed into Client.WriteAsync method together with the database name:
    //            foreach (var point in pointToWrite)
    //            {
    //                var response = await influxDbClient.WriteAsync(databaseName, point);
    //            }
    //            Logger.Log("DONE writing to influx");
    //        }
    //        catch (Exception ex)
    //        {

    //            Logger.Log(ex.Message);
    //            Logger.Log(ex.InnerException?.Message);
    //            Logger.Log(ex.StackTrace);
    //        }

    //    }

    //    private Point[] GeneratePoints(InternetBankingClientMobileTrackerobj currentObj)
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
    //                Measurement = "MobileClientTrxGabetest",
    //                Tags = new Dictionary<string, object>()
    //                {
    //                    {"InstitutionCode", GetGroup(currentObj.InstitutionCode)},
    //                    {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
    //                    { "Response", response.Key },
    //                    { "ProcessName", currentObj.ProcessName},
    //                    { "ProcessStatus", currentObj.ProcessStatus},
    //                    {"Environment" ,  this.environment },

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
    //                    {"TotalCnt", 1},
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


    //    public class InternetBankingClientMobileTrackerobj
    //    {
    //        public string TransactionId { get; set; }
    //        public string FlowID { get; set; }
    //        public string ProcessName { get; set; }
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
