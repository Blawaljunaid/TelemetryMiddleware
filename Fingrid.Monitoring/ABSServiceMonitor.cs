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
    public abstract class ABSServiceMonitor : BaseProcessor
    {

        string environment;
        string tableName = "";
        string channelName = "";
     //   Dictionary<string, string> technicalFailureCodes = null;
        Dictionary<string, string> SuccessStatuses = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;

        public ABSServiceMonitor(Loader loader, string channelName, string environmentName)
            : base(loader, "MobileAndCreditClubTransactions", channelName)
        {
            this.objDict = new ConcurrentDictionary<string, ABSServiceMonitorObj>();
            this.tableName = "Trx1";
            this.channelName = channelName;
            SuccessStatuses = new Dictionary<string, string>();
            string concSuccessStatuses = ConfigurationManager.AppSettings["ABServiceMonitoringSuccessStatus"];
            foreach (string s in concSuccessStatuses.Split(','))
            {
                SuccessStatuses.Add(s.Trim().ToLower(), s.Trim().ToLower());
            }

            testInstitutionCodesToSkip = new Dictionary<string, string>();
            string testInstitutionCodes = ConfigurationManager.AppSettings["TestInstitutionCodes"];
            foreach (string s in testInstitutionCodes.Split(','))
            {
                testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            }

            this.environment = environmentName;


        }


        ConcurrentDictionary<string, ABSServiceMonitorObj> objDict = null;

       

        protected override async void BreakMessageAndFlush(string message)
        {
           Console.WriteLine("**************************************************");
           Console.WriteLine("Message Received: " + message);
            //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
            message = message.Trim('"', ' ');
            //Console.WriteLine(message);

            string[] inputParam = message.Split(',');
            //publishInfo.UniqueId,  publishInfo.InstitutionCode, publishInfo.IsResponse.ToString(),publishInfo.Status, publishInfo.CurrentProcessingPoint,publishInfo.TransactionType,publishInfo.TransactionResponseCode,
            // DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss:fffffff tt")

            //d5cdbfd6-3291-4e03-8ecf-d9db5ebf04ca,100567,True,Successful,Final,AccountOpening,,31-01-2018 13:04:11:5788398 PM
            try
            {
                ABSServiceMonitorObj obj = new ABSServiceMonitorObj
                {
                    TransactionTime = DateTime.ParseExact(inputParam[7].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    TransactionType = string.IsNullOrEmpty(inputParam[5].Trim()) ? "None" : inputParam[3].Trim().Replace("TransactionType", ""),
                    CurrentProcessingPoint = inputParam[4].Trim() ,
                    IsResponse = Convert.ToBoolean(inputParam[2].Trim()),
                    Institution = inputParam[1].Trim(),
                    ResponseCode =string.IsNullOrEmpty( inputParam[6].Trim()) ? "None" : inputParam[3].Trim(),
                    UniqueId = inputParam[0].Trim(),
                    Status = inputParam[3].Trim()
                };


                //Trace.TraceInformation("Object {0},{1},{2},{3},{4} converted.", obj.UniqueId, obj.IsReponse, obj.PostingResponse, obj.PostingType, obj.TransactionTime);
               Console.WriteLine("{7} Object {0},{1},{2},{3},{4},{5},{6} converted.s {8}", obj.UniqueId, obj.TransactionTime, obj.Status, obj.IsResponse, obj.ResponseCode, obj.Institution, obj.CurrentProcessingPoint, this.channelName, IsSuccessful(obj.Status));


                if (testInstitutionCodesToSkip.ContainsKey(obj.Institution))
                {
                    return;
                }





                var requestOrResponsePoint = GenerateRequestOrResponsePoint(obj);
                try
                {
                    Console.WriteLine($"---Logging Request or Response--- {obj.UniqueId}");

                    //Trace.TraceInformation("Influx DB is {0}null. Point is {1}null.", (influxDbClient == null ? "" : "NOT "), (pointToWrite == null ? "" : "NOT "));
                    var response2 = await influxDbClient.WriteAsync(databaseName, requestOrResponsePoint);
                    Console.WriteLine($"---Done Logging Request or Response--- {obj.UniqueId}");
                }
                catch (Exception ex)
                {
                    Logger.Log("Issue writing to influx DB");
                    Console.WriteLine($"Error on Saving Request/Response {obj.UniqueId}   Msg: {ex.Message} Stacktrace {ex.StackTrace}");
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



                ABSServiceMonitorObj initialRequestObj = null;

                //Remove.
                if (!this.objDict.TryRemove(obj.UniqueId, out initialRequestObj))
                {
                   Console.WriteLine($"Error : Could not locate initial request {obj.UniqueId}");
                    return;
                }

                try
                {
                     Console.WriteLine($"---Logging TRX 1--- {obj.UniqueId}");
                    var pointToWrite = GeneratePoint(obj, initialRequestObj);
                    //Point is then passed into Client.WriteAsync method together with the database name:

                    var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                   Console.WriteLine($"--- Done Logging TRX 1--- {obj.UniqueId}");
                }
                catch (Exception ex)
                {
                    Logger.Log("Issue writing to influx DB");
                    Console.WriteLine($"Error occured on saving Trx1 {obj.UniqueId}  Msg:{ex.Message}  Stacktrace: { ex.StackTrace}");
                    Environment.Exit(1); // Exit with a non-zero status code so that the docker container can restart
                    // if for any reason we switch back to using this as a windows service
                    // you will have to comment that Exit() line of code in all the other files to avoid service
                    // crashing anytime inlfux has connection issue
                }
                try
                {
                  

                }
                catch (Exception ex)

                {
                   
                }

               Console.WriteLine($"---End--- {obj.UniqueId} \n\n");
            }
            catch (Exception ex)
            {
               Console.WriteLine($"---End---An error occure while parsing the message {message} \n\n");

            }

        }



        private Point GenerateRequestOrResponsePoint(ABSServiceMonitorObj currentObj)
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
                    {"Status",  currentObj.Status},
                    {"CurrentProcessingPoint", currentObj.CurrentProcessingPoint},

                },

                Fields = new Dictionary<string, object>()
                {
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        private Point GeneratePoint(ABSServiceMonitorObj currentObj, ABSServiceMonitorObj initialObj)
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
                    {"IsSuccessful", IsSuccessful(currentObj.Status) ? "Successful" : "Failed" },
                    {"InstitutionGroup" , GetGroup(currentObj.Institution) },
                    {"TransactionType", currentObj.TransactionType},
                    {"CurrentProcessingPoint", currentObj.CurrentProcessingPoint},
                    {"Status", currentObj.Status}
                },

                Fields = new Dictionary<string, object>()
                {
                    { "Time", currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds },
                    { "SuccessCnt", IsSuccessful(currentObj.Status) ? 1 : 0 },
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        private bool IsSuccessful(string status)
        {
            if (!String.IsNullOrEmpty(status) && !this.SuccessStatuses.ContainsKey(status.Trim().ToLower()))
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

        public class ABSServiceMonitorObj
        {
            //public string TransactionTime { get; set; }
            public bool IsResponse { get; set; }
            public string ResponseCode { get; set; }
            public string Institution { get; set; }
            public string CurrentProcessingPoint { get; set; }
            public string TransactionType { get; set; }
            public string Status { get; set; }

            public DateTime TransactionTime { get; set; }
            public string UniqueId { get; set; }

        }




    }
}

