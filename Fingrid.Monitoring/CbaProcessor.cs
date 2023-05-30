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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{

    public class CbaProcessor: BaseCbaProcessor
    {
        public CbaProcessor (Loader loader, List<Institution> institutions) :
            base(loader, "CbaTransactions", "__Monitoring.BankOne.Postings.Live", institutions)
            //base(loader, "CbaTransactions", "Monitoring.BankOne.Postings.API")
        {
            Logger.Log("CbaProcessor starting");
        }
    }

    public class CbaIsoProcessor : BaseCbaProcessor
    {
        public CbaIsoProcessor(Loader loader, List<Institution> institutions) :
            base(loader, "CbaTransactions2", "Monitoring.BankOne.ISO8583.Live", institutions)
        {
            Logger.Log("CbaIsoProcessor starting");
        }
    }
    public class CbaIsoProcessor2 : BaseCbaProcessor
    {
        public CbaIsoProcessor2(Loader loader, List<Institution> institutions) :
            base(loader, "CbaTransactions3", "Monitoring.BankOne.ISO8583Inner.Live", institutions)
        {
            Logger.Log("CbaIsoProcessor2 starting");
        }
    }
    public class BaseCbaProcessor : BaseProcessor
    {
        Dictionary<string, string> postingType = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;
        Dictionary<string, Institution> institutionsDict = null;
        // The Constructor
        public BaseCbaProcessor(Loader loader, string databaseName, string channel, List<Institution> institutions)
            : base(loader, databaseName, channel)
        {

            Logger.Log("BaseCbaProcessor starting");
            string postingTypeFileName = ConfigurationManager.AppSettings["PostingTypeFileName"];
            if (!System.IO.File.Exists(postingTypeFileName))
            {
                throw new Exception(String.Format("Posting type File doesn't exist. Please check the location and try again. '{0}'", postingTypeFileName));
            }

            postingType = GetFileContentsToDictionary(postingTypeFileName);
            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
            }
            Logger.Log("BaseCbaProcessor Middle");
            this.objDict = new ConcurrentDictionary<string, SampleObj>();
            testInstitutionCodesToSkip = new Dictionary<string, string>();
            string testInstitutionCodes = ConfigurationManager.AppSettings["TestInstitutionCodes"];
            foreach (string s in testInstitutionCodes.Split(','))
            {
                testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            }
            Logger.Log("BaseCbaProcessor End");
        }

        private Dictionary<string, string> GetFileContentsToDictionary(string fileName)
        {
            Dictionary<string, string> commaSeparatedContents = new Dictionary<string, string>();
            foreach (string s in System.IO.File.ReadAllLines(fileName))
            {
                string[] splitString = s.Split(',');
                if (!commaSeparatedContents.ContainsKey(splitString[0]))
                {
                    commaSeparatedContents.Add(splitString[0].Trim(), splitString[1].Trim());
                    Logger.Log("{0}, {1} added.", splitString[0].Trim(), splitString[1].Trim());
                }
            }

            return commaSeparatedContents;
        }


        ConcurrentDictionary<string, SampleObj> objDict = null;
        protected override void BreakMessageAndFlush(string message)
        {

            //Logger.Log("");
            //Logger.Log("Writing to my own guy influx");
            //var pointToWrite2 = GeneratePointDummy();
            //WriteToInflux(pointToWrite2);
            //Logger.Log("Done writing to my own guy influx");
            Logger.Log("");
            Logger.Log(message);
            //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
            message = message.Trim('"', ' ');
            Logger.Log(message);

            //100567,,About to Post,17,1611171114347797962,1,
            string[] inputParam = message.Split(',');
            if (inputParam.Length == 9)
            {
                Logger.Log("Skipping");
                Logger.Log("");
                return;
            }
            Logger.Log("Length of message {0}", inputParam.Length);
                SampleObj obj = new SampleObj
            {
                Institution = inputParam[0].Trim(),
                UniqueId = inputParam[1].Trim(),
                TransactionTime = DateTime.ParseExact(inputParam[2].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                IsReponse = Convert.ToBoolean(inputParam[3].Trim()),
                PostingResponse = inputParam[4].Trim(),
                PostingType = inputParam[5].Trim(),
                UniqueLogId = inputParam[6].Trim(),
                Number = inputParam[7].Trim(),
                ErrorCode = inputParam[8].Trim(),
                PostingId = inputParam[9].Trim(),
                CreditAmount = inputParam[10].Trim(),
                DebitAmount = inputParam[11].Trim(),
            };
            if (string.IsNullOrEmpty(obj.UniqueLogId)) obj.UniqueLogId = obj.UniqueId;

            if (String.IsNullOrEmpty(obj.UniqueId))
            {
                Logger.Log("No UniqueID exists for this transaction leg.");
                Logger.Log("");
                return;
            }

            //if (inputParam.Length > 7)
            //{
            //    obj.ErrorCode = inputParam[7];
            //}

            //Ensure ErrorCode ALWAYS exists. 
            if (String.IsNullOrEmpty(obj.ErrorCode)) obj.ErrorCode = "00";


            Logger.Log("Object {0},{1},{2},{3},{4} converted.", obj.UniqueId, obj.IsReponse, obj.PostingResponse, obj.PostingType, obj.TransactionTime);
            if (testInstitutionCodesToSkip.ContainsKey(obj.Institution))
            {
                Logger.Log("This is a code to skip");
                return;
            }

            //commented by gabe
            //if (obj.Number == "1" || obj.Number == "3")
            //{
            //    var pointToWrite2 = GenerateRequestOrResponsePoint(obj);
            //    Logger.Log("Writing to influx");
            //    WriteToInflux(pointToWrite2);
            //}


            if(obj.IsReponse == false)
            {
                Logger.Log("Catching the request object for id {0}", obj.UniqueId);
                this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                Logger.Log("");
                return;
            }

            //Comment by gabe
            //if (obj.Number == "1")
            //{
            //    Logger.Log("i am in the AddOrUpdate check for number 1");
            //    this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
            //    return;
            //}
            //if (obj.Number == "2")
            //{
            //    Logger.Log("i am in the AddOrUpdate check for number 2");
            //    this.objDict.AddOrUpdate(obj.UniqueId + "2", obj, (val1, val2) => obj);
            //    return;
            //}

            SampleObj initialRequestObj = null;

            if (!this.objDict.TryRemove(obj.UniqueId, out initialRequestObj))
            {
                Logger.Log("Moving the request object into initialRequestObj");
                Logger.Log("");
                return;
            }

            //Commented by gabe.
            //SampleObj initialRequestObj = null;
            //SampleObj secondObj = null;
            //if (!this.objDict.TryRemove(obj.UniqueId, out initialRequestObj))
            //{
            //    Logger.Log("i am in the TryRemove check for initialRequestObj");
            //    Logger.Log(initialRequestObj.ToString());
            //    return;
            //}
            //if (!this.objDict.TryRemove(obj.UniqueId + "2", out secondObj))
            //{
            //    Logger.Log("i am in the TryRemove check for secondObj");
            //    Logger.Log(secondObj.ToString());
            //    return;
            //}

            //commented by gabe
            //var pointToWrite = GeneratePoint(obj, initialRequestObj, secondObj);

            var pointToWrite = GeneratePoint(obj, initialRequestObj);

            WriteToInflux(pointToWrite);

            Logger.Log("");


        }



        private Object thisLock = new Object();

        public void Withdraw(decimal amount)
        {
            lock (thisLock)
            {
            }
        }

        //commented by gabe
        //private Point GenerateRequestOrResponsePoint(SampleObj currentObj)
        //{
        //    try
        //    {
        //        Logger.Log("i am in the GenerateRequestOrResponsePoint method");
        //        var pointToWrite = new Point()
        //        {
        //            Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
        //            Measurement = "Trx80", //Trx80 is a test measurment, change in production!!
        //            Tags = new Dictionary<string, object>()
        //        {
        //            //{ "Server", obj.Server },
        //            {"Institution", currentObj.Institution },
        //            //{"Response", currentObj.PostingResponse },
        //            //{"Type", currentObj.PostingType },
        //            {"InstitutionName",  InstitutionInfo.GetInstitutionName(currentObj.Institution,this.institutionsDict) },
        //            {"InstitutionGroup" , GetGroup(currentObj.Institution) },
        //            {"TransactionType",  GetTransactionType(currentObj.PostingType)},
        //            //{"ErrorCode", currentObj.ErrorCode },
        //            {"TechnicalSuccess", IsSuccessful(currentObj) ? "Successful" : "Failed" },
        //            {"IsRequest", currentObj.Number == "1" ? "1"  : "2"},
        //            {"PostingID", currentObj.PostingId },

        //        },
        //            Fields = new Dictionary<string, object>()
        //        {
        //            { "TotalCnt", 1 },
        //        },
        //            Timestamp = DateTime.UtcNow
        //        };
        //        Logger.Log("No Error: Returning point");
        //        return pointToWrite;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log("Error Generating a point in GenerateRequestOrResponsePoint method to send to influx db: " + ex.Message.ToString());
        //        var pointToWrite = new Point();
        //        return pointToWrite;
        //    }
        //}


        private Point GeneratePoint(SampleObj currentObj, SampleObj initialObj)
        {
            try
            {
                Logger.Log("i am in the GeneratePoint method");
                Logger.Log(currentObj.DebitAmount);
                Logger.Log(currentObj.CreditAmount);
                Logger.Log("Posting/Transaction Type: {0}",GetTransactionType(currentObj.PostingType));
                double totalMillisecs = currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds;
                var pointToWrite = new Point()
                {
                    Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                    Measurement = "Trx91", //Trx90 is a test measurment, change in production!!
                    Tags = new Dictionary<string, object>()
                {
                    
                       //{ "Server", obj.Server },
                    {"Institution", currentObj.Institution },
                    {"Response", currentObj.PostingResponse },
                    {"Type", currentObj.PostingType },
                    {"InstitutionGroup" , GetGroup(currentObj.Institution) },
                    {"TransactionType",  GetTransactionType(currentObj.PostingType)},
                    {"ErrorCode", currentObj.ErrorCode },
                    {"TechnicalSuccess", IsSuccessful(currentObj) ? "Successful" : "Failed" },
                    {"InstitutionName",  InstitutionInfo.GetInstitutionName(currentObj.Institution,this.institutionsDict) },
                    {"PostingID", currentObj.PostingId },


                },
                    Fields = new Dictionary<string, object>()
                {
                    {"TimeTaken",  totalMillisecs},
                    {"UniqueId",  currentObj.UniqueLogId},
                    {"Time", totalMillisecs },
                    {"InstitutionCode", currentObj.Institution },
                    {"CreditAmount",float.Parse(currentObj.CreditAmount) },
                    {"DebitAmount", float.Parse(currentObj.DebitAmount) },
                    //{ "Time2", currentObj.TransactionTime.Subtract(secondObj.TransactionTime).TotalMilliseconds },
                    {"SuccessCnt", IsSuccessful(currentObj) ? 1 : 0 },
                    {"TotalCnt", 1},
                },
                    Timestamp = DateTime.UtcNow
                };
                Logger.Log("No Error: Returning point");
                return pointToWrite;
            }
            catch (Exception ex)
            {
                Logger.Log("Error Generating a point in GeneratePoint method to send to influx db: " + ex.Message.ToString());
                var pointToWrite = new Point();
                return pointToWrite;
            }
        }



        //Commented by gabe
        //private Point GeneratePoint(SampleObj currentObj, SampleObj initialObj, SampleObj secondObj)
        //{
        //    try
        //    {
        //        Logger.Log("i am in the GeneratePoint method");
        //        double totalMillisecs = currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds;
        //        var pointToWrite = new Point()
        //        {
        //            Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
        //            Measurement = "Trx90", //Trx90 is a test measurment, change in production!!
        //            Tags = new Dictionary<string, object>()GetInstitutionName(currentObj.Institution)
        //        {

        //               //{ "Server", obj.Server },
        //            {"Institution", currentObj.Institution },
        //            {"Response", currentObj.PostingResponse },
        //            {"Type", currentObj.PostingType },
        //            {"InstitutionGroup" , GetGroup(currentObj.Institution) },
        //            {"TransactionType",  GetTransactionType(currentObj.PostingType)},
        //            {"ErrorCode", currentObj.ErrorCode },
        //            {"TechnicalSuccess", IsSuccessful(currentObj) ? "Successful" : "Failed" },
        //            {"InstitutionName",  InstitutionInfo.GetInstitutionName(currentObj.Institution,this.institutionsDict) },
        //            {"PostingID", currentObj.PostingId },


        //        },
        //            Fields = new Dictionary<string, object>()
        //        {
        //            {"TimeTaken",  totalMillisecs},
        //            {"UniqueId",  currentObj.UniqueLogId},
        //            { "Time", totalMillisecs },
        //            {"InstitutionCode", currentObj.Institution },
        //            //{ "Time2", currentObj.TransactionTime.Subtract(secondObj.TransactionTime).TotalMilliseconds },
        //            { "SuccessCnt", IsSuccessful(currentObj) ? 1 : 0 },
        //            {"TotalCnt", 1},
        //        },
        //            Timestamp = DateTime.UtcNow
        //        };
        //        Logger.Log("No Error: Returning point");
        //        return pointToWrite;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log("Error Generating a point in GeneratePoint method to send to influx db: " + ex.Message.ToString());
        //        var pointToWrite = new Point();
        //        return pointToWrite;
        //    }
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




        //Gabriel CODE

        private Point GeneratePointDummy()
        {
            try
            {
                Logger.Log("i am in the GenerateRequestOrResponsePoint method");
                var pointToWrite = new Point()
                {
                    Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                    Measurement = "TrxTest4",
                    Tags = new Dictionary<string, object>()
                {

                    {"TechnicalSuccess","Test" },
                    {"TechnicalSuccess2","Test2" },
                    {"TechnicalSuccess3","Test3" },
                    {"TechnicalSuccess4","Test4" },


                },
                    Fields = new Dictionary<string, object>()
                {
                    { "TotalCnt", 1 },
                },
                    Timestamp = DateTime.UtcNow
                };
                Logger.Log("No Error: Returning point");
                return pointToWrite;
            }
            catch (Exception ex)
            {
                Logger.Log("Error Generating a point in GenerateRequestOrResponsePoint to send to influx db: " + ex.Message.ToString());
                var pointToWrite = new Point();
                return pointToWrite;
            }
        }
        private async void WriteToInflux(Point pointToWrite)
        {
            try
            {
                Logger.Log("i am in the writToInflux method");
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                Logger.Log("Done with writing to influx");

            }
            catch (Exception ex)
            {
                Logger.Log("Wierd Error sort of");
                Logger.Log(string.Format("Error when writing to influx db: {0}", ex.InnerException + ex.Message + ex.StackTrace));
                Environment.Exit(1); // Exit with a non-zero status code so that the docker container can restart
                                                    // if for any reason we switch back to using this as a windows service
                                                    // you will have to comment that Exit() line of code in all the other files to avoid service
                                                    // crashing anytime inlfux has connection issue
            }
        }


        //END Gabriel CODE

        private bool IsSuccessful(SampleObj obj)
        {
            if (obj.PostingResponse == "Failed" && (obj.ErrorCode == "-1" || String.IsNullOrEmpty(obj.ErrorCode)))
                return false;

            return true;
        }

        //Commented by gabe
        //private string GetTransactionType(string postingType)
        //{
        //    switch (postingType)
        //    {
        //        case "17":
        //            return "E-Channels";
        //        default:
        //            return "Non E-channels"; 
        //    }
        //}

        private string GetTransactionType(string postingType)
        {
            string postingTypeName = "";

            if (this.postingType.TryGetValue(postingType, out postingTypeName))
            {
                return postingTypeName;
            }

            if (String.IsNullOrEmpty(postingTypeName))
            {
                postingTypeName = "NA";
            }

            return postingTypeName;
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

                default:
                    return "Others";
            }
        }

        public class SampleObj
        {
            //public string TransactionTime { get; set; }
            public string PostingResponse { get; set; }
            public DateTime TransactionTime { get; set; }
            public string Institution { get; set; }
            public string UniqueId { get; set; }
            public string Server { get; set; }
            public bool IsReponse { get; set; }
            public string PostingType { get; set; }
            public string UniqueLogId { get; set; }
            public string Number { get; set; }
            public string ErrorCode { get; set; }
            public string PostingId { get; set; }
            public string CreditAmount { get; set; }
            public string DebitAmount { get; set; }

        }

        public void Stop()
        {
            //if (redis != null)
            //{
            //    redis.Close();
            //}
        }


    }
}



//public class RedisPubSub
//{
//    static ICacheClient cacheClient = null;
//    static IRedisCacheStorage cacheStorage = null;
//    private string channelKey = "Fingrid.Channels";
//    public RedisPubSub()
//    {
//        if (cacheClient == null) cacheClient = new StackExchangeRedisCacheClient(new StackExchange.Redis.Extensions.Jil.JilSerializer());
//        if (cacheStorage == null) cacheStorage = new Fingrid.Infrastructure.Common.Caching.Redis.RedisExtendedCacheStorage(cacheClient);
//    }
//    public void Publish(string name, string message)
//    {
//        cacheStorage.CacheClient.Publish(name, "cacheStorage " + message);
//        cacheClient.Publish(name, "cacheClient " + message);
//    }
//    public void Subscribe(string name, Action<string> handler)
//    {
//        cacheClient.Subscribe(name, handler);
//    }
//    public List<string> GetAllChannels()
//    {
//        var results = cacheClient.Database.SetMembers(channelKey)?.Select(y => y.ToString()).ToList();
//        return results ?? new List<string>();
//    }
//}
