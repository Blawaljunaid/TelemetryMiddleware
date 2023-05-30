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
    public abstract class MobileProcessor : BaseProcessor
    {

        string environment;
        string tableName = "";
        Dictionary<string, string> technicalFailureCodes = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;
        Dictionary<string, List<string>> InstitutionSpecificTechnicalSuccessCodes = null;

        public MobileProcessor(Loader loader, string channelName, string environmentName)
            : base(loader, "MobileAndCreditClubTransactions", channelName)
        {
            this.objDict = new ConcurrentDictionary<string, MobileObj>();
            this.tableName = "Trx1";
            technicalFailureCodes = new Dictionary<string, string>();
            string concatenatedTechnicalSuccess = ConfigurationManager.AppSettings["MobileTechnicalFailureCodes"];
            foreach (string s in concatenatedTechnicalSuccess.Split(','))
            {
                technicalFailureCodes.Add(s.Trim(), s.Trim());
            }

            testInstitutionCodesToSkip = new Dictionary<string, string>();
            string testInstitutionCodes = ConfigurationManager.AppSettings["TestInstitutionCodes"];
            foreach (string s in testInstitutionCodes.Split(','))
            {
                testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            }


            string institutionSpecificTechnicalSuccessCodes = ConfigurationManager.AppSettings["InstitutionSpecificTechnicalSuccessCodes"];
            InstitutionSpecificTechnicalSuccessCodes = new Dictionary<string, List<string>>();
            //000000:00,12,13; 111111:00,34;"000000:00,12,13;111111:00,34;"
            foreach (string s in institutionSpecificTechnicalSuccessCodes.Split(';'))
            {
                var split = s.Split(':');
                if (split.Length > 1)
                    InstitutionSpecificTechnicalSuccessCodes.Add(split[0].Trim(), split[1].Split(',').ToList());

            }
            this.environment = environmentName;


        }


        ConcurrentDictionary<string, MobileObj> objDict = null;

        protected virtual string GetUniqueId(string[] inputParams)
        {
            return inputParams[0].Trim();
        }

        protected override async void BreakMessageAndFlush(string message)
        {
            Logger.Log("**************************************************");
            Trace.TraceInformation("**************************************************");
            Trace.TraceInformation("Message Received: " + message);
            //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
            message = message.Trim('"', ' ');
            // Trace.TraceInformation(message);

            string[] inputParam = message.Split(',');

            try
            {
                MobileObj obj = new MobileObj
                {
                    TransactionTime = DateTime.ParseExact(inputParam[7].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    TransactionType = inputParam[2].Trim().Replace("TransactionType", ""),
                    SubTransactionType = string.IsNullOrEmpty(inputParam[3]) ? "None" : inputParam[3].Trim().Replace("TransactionType", ""),
                    IsResponse = Convert.ToBoolean(inputParam[4].Trim()),
                    Institution = inputParam[1].Trim(),
                    ResponseCode = inputParam[5].Trim(),
                    UniqueId = GetUniqueId(inputParam),
                };


                //Trace.TraceInformation("Object {0},{1},{2},{3},{4} converted.", obj.UniqueId, obj.IsReponse, obj.PostingResponse, obj.PostingType, obj.TransactionTime);
                Trace.TraceInformation("{7} Object {0},{1},{2},{3},{4},{5},{6} converted.s {8}", obj.UniqueId, obj.TransactionTime, obj.TransactionType, obj.IsResponse, obj.ResponseCode, obj.Institution, obj.SubTransactionType, this.environment, IsSuccessful(obj.ResponseCode, obj.Institution));


                if (testInstitutionCodesToSkip.ContainsKey(obj.Institution))
                {
                    return;
                }





                var requestOrResponsePoint = GenerateRequestOrResponsePoint(obj);
                try
                {
                     Trace.TraceInformation($"---Logging Request or Response--- {obj.UniqueId}");

                    //Trace.TraceInformation("Influx DB is {0}null. Point is {1}null.", (influxDbClient == null ? "" : "NOT "), (pointToWrite == null ? "" : "NOT "));
                    var response2 = await influxDbClient.WriteAsync(databaseName, requestOrResponsePoint);
                    Trace.TraceInformation($"---Done Logging Request or Response--- {obj.UniqueId}");
                }
                catch (Exception ex)
                {
                    Logger.Log("Issue writing to influx DB");
                    Trace.TraceInformation($"Error on Saving Request/Response {obj.UniqueId}   Msg: {ex.Message} Stacktrace {ex.StackTrace}");
                    Environment.Exit(1); // Exit with a non-zero status code so that the docker container can restart
                    // if for any reason we switch back to using this as a windows service
                    // you will have to comment that Exit() line of code in all the other files to avoid service
                    // crashing anytime inlfux has connection issue
                }

                if (!obj.IsResponse)
                {
                    this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                    return;

                }



                MobileObj initialRequestObj = null;

                //Remove.
                if (!this.objDict.TryRemove(obj.UniqueId, out initialRequestObj))
                {
                    Trace.TraceInformation($"Error : Could not locate initial request {obj.UniqueId}");
                    return;
                }

                try
                {
                    Trace.TraceInformation($"---Logging TRX 1--- {obj.UniqueId}");
                    var pointToWrite = GeneratePoint(obj, initialRequestObj);
                    //Point is then passed into Client.WriteAsync method together with the database name:

                    var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                    Trace.TraceInformation($"--- Done Logging TRX 1--- {obj.UniqueId}");
                }
                catch (Exception ex)
                {
                    Logger.Log("Issue writing to influx DB");
                    Trace.TraceInformation($"Error occured on saving Trx1 {obj.UniqueId}  Msg:{ex.Message}  Stacktrace: { ex.StackTrace}");
                    Environment.Exit(1); // Exit with a non-zero status code so that the docker container can restart
                    // if for any reason we switch back to using this as a windows service
                    // you will have to comment that Exit() line of code in all the other files to avoid service
                    // crashing anytime inlfux has connection issue
                }

                Trace.TraceInformation($"---End--- {obj.UniqueId} \n\n");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"---End---An error occure while parsing the message {message} \n\n");

            }

        }



        private Point GenerateRequestOrResponsePoint(MobileObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx2", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                     {"Environment" ,  this.environment },
                    {"Institution", currentObj.Institution },
                    {"InstitutionGroup" , GetGroup(currentObj.Institution) },
                    {"TransactionType",  currentObj.TransactionType},
                    {"IsRequest", currentObj.IsResponse==false ?"1":"2"},
                    {"SubTransactionType", currentObj.SubTransactionType},

                },

                Fields = new Dictionary<string, object>()
                {
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        private Point GeneratePoint(MobileObj currentObj, MobileObj initialObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = this.tableName, // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    {"Environment" ,  this.environment },
                    {"Institution", currentObj.Institution },
                    {"ResponseCode", currentObj.ResponseCode },
                    {"TechnicalSuccess", IsSuccessful(currentObj.ResponseCode,currentObj.Institution) ? "Successful" : "Failed" },
                    {"InstitutionGroup" , GetGroup(currentObj.Institution) },
                    {"TransactionType", currentObj.TransactionType},
                    {"SubTransactionType", currentObj.SubTransactionType},

                },

                Fields = new Dictionary<string, object>()
                {
                    { "Time", currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds },
                    { "SuccessCnt", IsSuccessful(currentObj.ResponseCode,currentObj.Institution) ? 1 : 0 },
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        private bool IsSuccessful(string responseCode, string institutionCode)
        {
            List<string> successList = null;
            if (InstitutionSpecificTechnicalSuccessCodes.TryGetValue(institutionCode, out successList) && !string.IsNullOrEmpty(responseCode))
            {
                if (successList.Contains(responseCode))
                    return true;
                else
                    return false;
            }
            else
            if (!String.IsNullOrEmpty(responseCode) && this.technicalFailureCodes.ContainsKey(responseCode.Trim()))
            {
                return false;
            }
            return true;
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

        public class MobileObj
        {
            //public string TransactionTime { get; set; }
            public bool IsResponse { get; set; }
            public string ResponseCode { get; set; }
            public string Institution { get; set; }

            public string SubTransactionType { get; set; }
            public string TransactionType { get; set; }
            public DateTime TransactionTime { get; set; }
            public string UniqueId { get; set; }

        }




    }
}

