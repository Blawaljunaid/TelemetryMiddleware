using Fingrid.Monitoring.Utility;
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
    public class UssdProcessor : BaseProcessor
    {
        Dictionary<string, string> testInstitutionCodesToSkip = null;
        Dictionary<string, Institution> institutionsDict = null;
        Dictionary<string, string> technicalSuccesses = null;
        Dictionary<string, string> integrationFIs = null;
        public UssdProcessor(Loader loader, List<Institution> institutions)
            : base(loader, "UssdTransactions", "__Monitoring.BankOne.USSD.Live")
        {
            this.objDict = new ConcurrentDictionary<string, UssdObj>();
            //testInstitutionCodesToSkip = new Dictionary<string, string>();
            //string testInstitutionCode = ConfigurationManager.AppSettings["USSDTestInstitutionCodes"];
            //foreach (string s in testInstitutionCode.Split(','))
            //{
            //    //testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            //}
            Logger.Log("Entered USSD1");

            testInstitutionCodesToSkip = new Dictionary<string, string>();
            string testInstitutionCodes = ConfigurationManager.AppSettings["USSDTestInstitutionCodes"];
            foreach (string s in testInstitutionCodes.Split(','))
            {
                testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            }
            Logger.Log("Entered USSD2");

            technicalSuccesses = new Dictionary<string, string>();
            string concatenatedTechnicalSuccess = ConfigurationManager.AppSettings["USSDTechnicalSuccessCodes"];
            foreach (string s in concatenatedTechnicalSuccess.Split(','))
            {
                technicalSuccesses.Add(s.Trim(), s.Trim());
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

            Logger.Log("Entered USSD4");
            try
            {
                this.institutionsDict = new Dictionary<string, Institution>();
                foreach (var institution in institutions)
                {
                    //Logger.Log("Entered USSD5");
                    Logger.Log($"{institution.InstitutionCode}");

                    if (!this.institutionsDict.ContainsKey(institution.InstitutionCode))
                        this.institutionsDict.Add(institution.InstitutionCode, institution);
                }
                Logger.Log("Entered USSD3");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message + "\n" + ex.StackTrace);
            }
        }


        ConcurrentDictionary<string, UssdObj> objDict = null;
        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {
                Logger.Log(message);
                //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
                message = message.Trim('"', ' ');
                Logger.Log(message);

                string[] inputParam = message.Split(',');
                UssdObj obj = new UssdObj
                {

                    UniqueId = inputParam[0].Trim(),
                    Institution = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    PhoneNo = !string.IsNullOrEmpty(inputParam[2]) ? inputParam[2].Trim() : "NA",
                    Network = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    TransactionTime = DateTime.ParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    ResponseCode = !string.IsNullOrEmpty(inputParam[5]) ? inputParam[5].Trim() : "NA",
                    TransactionType = !string.IsNullOrEmpty(inputParam[7]) ? inputParam[7].Trim() : "NA",
                    IsTransaction = !string.IsNullOrEmpty(inputParam[8]) ? Convert.ToBoolean(inputParam[8].Trim()) : false,
                    IsRequest = inputParam[9].Trim(),
                };

                if (String.IsNullOrEmpty(obj.TransactionType))
                {
                    obj.TransactionType = "Unknown";
                }

                DateTime responseDate = DateTime.Now;
                if (DateTime.TryParseExact(inputParam[4].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out responseDate))
                {
                    obj.TransactionTime = responseDate;
                }
                else
                {
                    Logger.Log("Error parsing date {0}", inputParam[4].Trim());
                    return;
                }
                //Ensure ErrorCode ALWAYS exists. 
                //if (String.IsNullOrEmpty(obj.ErrorCode)) obj.ErrorCode = "00";


                Logger.Log("");
                Logger.Log("Object {0},{1},{2},{3},{4},{5},{6} converted.", obj.UniqueId, obj.PhoneNo, obj.Institution, obj.Network, obj.TransactionTime, obj.IsRequest, obj.TransactionType);
                //if (testInstitutionCodesToSkip.ContainsKey(obj.Institution))
                //{
                //    return;
                //}
                if (testInstitutionCodesToSkip.ContainsKey(obj.Institution))
                {
                    return;
                }

                var pointToWrite = GenerateRequestOrResponsePoint(obj);

                try
                {
                    //Logger.Log("Influx DB is {0}null. Point is {1}null.", (influxDbClient == null ? "" : "NOT "), (pointToWrite == null ? "" : "NOT "));
                    var response2 = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Table 1 {obj.TransactionType} is Request {obj.IsRequest} id {obj.UniqueId}---{ex.Message}---     \n {ex.StackTrace}");
                    Logger.Log($"{ex.Message} | {ex.InnerException?.Message} | {ex.InnerException?.InnerException?.Message} | {ex.InnerException?.InnerException?.InnerException?.Message}");
                }



                if (obj.IsRequest == "1")
                {
                    this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                    return;
                }
                //if (obj.TransactionType.ToLower() != "pinselection" || obj.TransactionType.ToLower() != "recharge")
                //{
                //    Logger.Log($"Not loggint {obj.TransactionType} with {obj.UniqueId} to db2");
                //    return;
                //}
                UssdObj initialRequestObj = null;

                //Remove.
                Logger.Log($"Ussd get from dict into {obj.UniqueId}");
                if (!this.objDict.TryRemove(obj.UniqueId, out initialRequestObj))
                {
                    return;
                }


                var pointToWrite2 = GeneratePoint(obj, initialRequestObj);
                //Point is then passed into Client.WriteAsync method together with the database name:
                try
                {
                    Logger.Log($"Inserting into Table 2 {obj.UniqueId}");
                    var response = await influxDbClient.WriteAsync(databaseName, pointToWrite2);
                    Logger.Log($"Done Inserting into Table 2 {obj.UniqueId}");

                }
                catch (Exception ex)
                {
                    Logger.Log($"Table 2 {obj.TransactionType} is Request {obj.IsRequest} id {obj.UniqueId}---{ex.Message}---     \n {ex.StackTrace}");

                }
            }
            catch (Exception ex)
            {
                Logger.Log($" 2---{ex.Message}---     \n {ex.StackTrace}");

                Logger.Log(ex.Message);
            }
        }

        private Object thisLock = new Object();

        public void Withdraw(decimal amount)
        {
            lock (thisLock)
            {
            }
        }


        private Point GenerateRequestOrResponsePoint(UssdObj currentObj)
        {
            Logger.Log($"Institution:{string.IsNullOrEmpty(currentObj.Institution)},InstitutionName {string.IsNullOrEmpty(GetInstitutionName(currentObj.Institution)?.ToString())}");
            Logger.Log($"Network:{string.IsNullOrEmpty(currentObj.Network)},TransactionType {string.IsNullOrEmpty(currentObj.TransactionType)}");
            Logger.Log($"PhoneNo:{string.IsNullOrEmpty(currentObj.PhoneNo)}");
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                        //{ "Server", obj.Server },
                    {"Institution", currentObj.Institution },
                    {"InstitutionName", GetInstitutionName(currentObj.Institution) },
                    {"Network", currentObj.Network },
                    {"TransactionType",  currentObj.TransactionType},
                },


                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    //{ "Time", currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds },
                    // { "Time2", currentObj.TransactionTime.Subtract(secondObj.TransactionTime).TotalMilliseconds },
                    //{ "SuccessCnt", IsSuccessful(currentObj) ? 1 : 0 },
                    {"PhoneNo" , currentObj.PhoneNo},
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }

        private Point GeneratePoint(UssdObj currentObj, UssdObj initialObj)
        {
            double hourOfDay = DateTime.Now.Subtract(DateTime.Today).TotalHours;
            bool isDayShift = hourOfDay >= 8 && hourOfDay <= 21;
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx2", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    {"Institution", currentObj.Institution },
                    {"InstitutionName", GetInstitutionName(currentObj.Institution) },
                    //{"RequestType", currentObj.Number == "1" ? 1  : 2},
                    {"Network", currentObj.Network },
                    {"TransactionType",  currentObj.TransactionType},
                    {"ResponseCode",  String.IsNullOrEmpty(currentObj.ResponseCode) ? "00" : currentObj.ResponseCode},
                    {"IsTransaction",  currentObj.IsTransaction? 1 : 0},
                    {"TechnicalSuccess", IsSuccessful(currentObj.ResponseCode) ? "Successful" : "Failed"},
                    {"Shift", isDayShift? "DayShift" : "NightShift"},
                    {"InstitutionGroup" , GetGroup(currentObj.Institution, this.integrationFIs) },

                },


                Fields = new Dictionary<string, object>()
                {
                    {"PhoneNo" , currentObj.PhoneNo},
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "Duration", currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds },
                    // { "Time2", currentObj.TransactionTime.Subtract(secondObj.TransactionTime).TotalMilliseconds },
                    { "SuccessCnt", IsSuccessful(currentObj.ResponseCode) ? 1 : 0 },
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }
        //private bool IsSuccessful(UssdObj obj)
        //{
        //    if (obj.PostingResponse == "Failed" && (obj.ErrorCode == "-1" || String.IsNullOrEmpty(obj.ErrorCode)))
        //        return false;

        //    return true;
        //}

        private bool IsSuccessful(string responseCode)
        {
            if (!String.IsNullOrEmpty(responseCode) && this.technicalSuccesses.ContainsKey(responseCode.Trim()))
            {
                return true;
            }
            return false;
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


        private string GetGroup(string institutionCode, Dictionary<string, string> integrationInstitutions)
        {
            if (integrationInstitutions.ContainsKey(institutionCode))
            {
                return "Integration FIs";
            }

            switch (institutionCode)
            {
                case "045":
                    return "DBN";

                case "240":
                    return "Sterling";

                case "265":
                    return "BOI";

                case "307":
                    return "Access";

                default:
                    return "Others";
            }

        }




        public class UssdObj
        {
            public string UniqueId { get; set; }
            public string PhoneNo { get; set; }
            public string Network { get; set; }
            public DateTime TransactionTime { get; set; }
            public string ResponseCode { get; set; }

            public string Institution { get; set; }
            public string TransactionType { get; set; }

            public bool IsTransaction { get; set; }

            public string IsRequest { get; set; }

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
