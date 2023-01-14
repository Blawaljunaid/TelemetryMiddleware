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
    public class InternetBankingProcessor : BaseProcessor
    {
        Dictionary<string, string> testInstitutionCodesToSkip = null;
        Dictionary<string, Institution> institutionsDict = null;
        Dictionary<string, string> technicalSuccesses = null;
        Dictionary<string, string> integrationFIs = null;

        public InternetBankingProcessor(Loader loader, List<Institution> institutions)
            : base(loader, "InternetBankingTransactions", "__Monitoring.InternetBanking.Live")
        {
            this.objDict = new ConcurrentDictionary<string, InternetBankingObj>();
            //testInstitutionCodesToSkip = new Dictionary<string, string>();
            //string testInstitutionCode = ConfigurationManager.AppSettings["USSDTestInstitutionCodes"];
            //foreach (string s in testInstitutionCode.Split(','))
            //{
            //    //testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            //}
            Logger.Log("Entered INTBANK");

            testInstitutionCodesToSkip = new Dictionary<string, string>();
            string testInstitutionCodes = ConfigurationManager.AppSettings["InternetBankingTestInstitutionCodes"];
            foreach (string s in testInstitutionCodes.Split(','))
            {
                testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            }
            Logger.Log("Entered INTBANK");

            technicalSuccesses = new Dictionary<string, string>();
            string concatenatedTechnicalSuccess = ConfigurationManager.AppSettings["InternetBankingSuccessCodes"];
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

            Logger.Log("Entered INTBANK");
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
                Logger.Log("Entered INTBANK");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message + "\n" + ex.StackTrace);
            }
        }

        ConcurrentDictionary<string, InternetBankingObj> objDict = null;

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
                var stringDate = DateTime.ParseExact(inputParam[8].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture);
                InternetBankingObj obj = new InternetBankingObj
                {

                    UniqueId = inputParam[0].Trim(),
                    InstitutionCode = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    TransactionType = !string.IsNullOrEmpty(inputParam[2]) ? inputParam[2].Trim() : "NA",
                    SubTransactionType = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    IsResponse = inputParam[4].Trim(),
                    Status = !string.IsNullOrEmpty(inputParam[5]) ? inputParam[5].Trim() : "NA",
                    TransactionResponseCode = !string.IsNullOrEmpty(inputParam[6]) ? inputParam[6].Trim() : "NA",
                    TransactionResponseDescription = !string.IsNullOrEmpty(inputParam[7]) ? inputParam[7].Trim() : "NA",
                    DateTime = stringDate,

                };

                if (String.IsNullOrEmpty(obj.TransactionType))
                {
                    obj.TransactionType = "Unknown";
                }

                DateTime responseDate = DateTime.Now;
                if (DateTime.TryParseExact(inputParam[8].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out responseDate))
                {
                    obj.DateTime = responseDate;
                }
                else
                {
                    Logger.Log("Error parsing date {0}", inputParam[8].Trim());
                    return;
                }
                //Ensure ErrorCode ALWAYS exists. 
                //if (String.IsNullOrEmpty(obj.ErrorCode)) obj.ErrorCode = "00";



                Logger.Log("InternetBanking Object {0},{1},{2},{3},{4},{5},{6} converted.", obj.UniqueId, obj.InstitutionCode, obj.TransactionType, obj.TransactionResponseDescription, obj.SubTransactionType, obj.IsResponse, obj.Status);
                

                if (testInstitutionCodesToSkip.ContainsKey(obj.InstitutionCode))
                {
                    return;
                }
                Logger.Log("Generate Request Or Response point to write to influx");
                var pointToWrite = GenerateRequestOrResponsePoint(obj);

                try
                {
                    Logger.Log("Start wriiting GenerateRequestOrResponsePoint to influx");
                    //Point is then passed into Client.WriteAsync method together with the database name:
                    var response2 = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                    Logger.Log("DONE writing GenerateRequestOrResponsePoint to influx");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Table 1 {obj.TransactionType} is Request {obj.IsResponse} id {obj.UniqueId}---{ex.Message}---     \n {ex.StackTrace}");
                    Logger.Log($"{ex.Message} | {ex.InnerException?.Message} | {ex.InnerException?.InnerException?.Message} | {ex.InnerException?.InnerException?.InnerException?.Message}");
                }



                if (obj.IsResponse == "False")
                {
                    Logger.Log("Recording Request object with id: {0}", obj.UniqueId);

                    this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                    return;
                }
                //if (obj.TransactionType.ToLower() != "pinselection" || obj.TransactionType.ToLower() != "recharge")
                //{
                //    Logger.Log($"Not loggint {obj.TransactionType} with {obj.UniqueId} to db2");
                //    return;
                //}
                InternetBankingObj initialRequestObj = null;

                Logger.Log("Recording response object with id: {0}", obj.UniqueId);

                if (!this.objDict.TryRemove(obj.UniqueId, out initialRequestObj))
                {
                    Logger.Log("Could not find response object with id: {0}", obj.UniqueId);

                    return;
                }

                Logger.Log("Sending response and request object to generate point to send to influx");

                var pointToWrite2 = GeneratePoint(obj, initialRequestObj);
                //Point is then passed into Client.WriteAsync method together with the database name:
                try
                {
                    Logger.Log("Start writing GeneratePoint to influx");
                    var response = await influxDbClient.WriteAsync(databaseName, pointToWrite2);
                    Logger.Log("DONE writing GeneratePoint to influx");

                }
                catch (Exception ex)
                {
                    Logger.Log($"Table 2 {obj.TransactionType} is Request {obj.IsResponse} id {obj.UniqueId}---{ex.Message}---     \n {ex.StackTrace}");

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


        private Point GenerateRequestOrResponsePoint(InternetBankingObj currentObj)
        {
            Logger.Log($"Institution:{string.IsNullOrEmpty(currentObj.InstitutionCode)},InstitutionName {string.IsNullOrEmpty(GetInstitutionName(currentObj.InstitutionCode)?.ToString())}");
            Logger.Log($"Status:{string.IsNullOrEmpty(currentObj.Status)},TransactionType {string.IsNullOrEmpty(currentObj.TransactionType)}");
            Logger.Log($"SubTransactionType:{string.IsNullOrEmpty(currentObj.SubTransactionType)}");
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                        //{ "Server", obj.Server },
                    {"Institution", currentObj.InstitutionCode },
                    {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
                    {"SubTransactionType", currentObj.SubTransactionType },
                    {"TransactionType",  currentObj.TransactionType},
                    {"Status", currentObj.Status },
                },


                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    //{ "Time", currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds },
                    // { "Time2", currentObj.TransactionTime.Subtract(secondObj.TransactionTime).TotalMilliseconds },
                    //{ "SuccessCnt", IsSuccessful(currentObj) ? 1 : 0 },
                    
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }

        private Point GeneratePoint(InternetBankingObj currentObj, InternetBankingObj initialObj)
        {
            double hourOfDay = DateTime.Now.Subtract(DateTime.Today).TotalHours;
            bool isDayShift = hourOfDay >= 8 && hourOfDay <= 21;
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx2", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    {"Institution", currentObj.InstitutionCode },
                    {"InstitutionName", GetInstitutionName(currentObj.InstitutionCode) },
                    //{"RequestType", currentObj.Number == "1" ? 1  : 2},
                    {"Status", currentObj.Status },
                    {"TransactionType",  currentObj.TransactionType},
                    {"SubTransactionType",  currentObj.SubTransactionType},
                    {"TransactionResponseDescription",  currentObj.TransactionResponseDescription},
                    {"ResponseCode",  String.IsNullOrEmpty(currentObj.TransactionResponseCode) ? "00" : currentObj.TransactionResponseCode},
                    {"TechnicalSuccess", IsSuccessful(currentObj.TransactionResponseCode) ? "Successful" : "Failed"},
                    {"Shift", isDayShift? "DayShift" : "NightShift"},
                    {"InstitutionGroup" , GetGroup(currentObj.InstitutionCode, this.integrationFIs) },

                },


                Fields = new Dictionary<string, object>()
                {
                    
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "Duration", currentObj.DateTime.Subtract(initialObj.DateTime).TotalMilliseconds },
                    // { "Time2", currentObj.TransactionTime.Subtract(secondObj.TransactionTime).TotalMilliseconds },
                    { "SuccessCnt", IsSuccessful(currentObj.TransactionResponseCode) ? 1 : 0 },
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




        public class InternetBankingObj
        {
            public string UniqueId { get; set; }
            public string InstitutionCode { get; set; }
            public string TransactionType { get; set; }
            public DateTime DateTime { get; set; }
            public string Status { get; set; }

            public string TransactionResponseCode { get; set; }
            public string TransactionResponseDescription { get; set; }

            public string IsResponse { get; set; }

            public string SubTransactionType { get; set; }

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
