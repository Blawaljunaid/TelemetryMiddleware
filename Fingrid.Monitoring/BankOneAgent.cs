﻿using Fingrid.Monitoring.Utility;
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
    public class BankOneAgent : BaseProcessor
    {
        Dictionary<string, Institution> institutionsDict = null;
        
        Dictionary<string, string> integrationFIs = null;
        Dictionary<string, string> BanKInTheBoxFIs = null;
        Dictionary<string, string> technicalSuccessItems = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;

        public BankOneAgent(Loader loader, List<Institution> institutions)
            : base(loader, "BankOneAgentTrx", "BankOneAgent")
        {
            this.objDict = new ConcurrentDictionary<string, BankOneAgentObj>();
            
            

            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
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

            testInstitutionCodesToSkip = new Dictionary<string, string>();
            string testInstitutionCodes = ConfigurationManager.AppSettings["TestInstitutionCodes"];



            foreach (string s in testInstitutionCodes.Split(','))
            {
                testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            }

            this.BanKInTheBoxFIs = new Dictionary<string, string>();





            string BanKInTheBoxFIsConcatenated = ConfigurationManager.AppSettings["BanKInTheBoxFIs"];
            if (!String.IsNullOrEmpty(BanKInTheBoxFIsConcatenated))
            {
                foreach (var s in BanKInTheBoxFIsConcatenated.Split(','))
                {
                    BanKInTheBoxFIs.Add(s.Trim(), s.Trim());
                }
            }


            technicalSuccessItems = new Dictionary<string, string>();
            string concatenatedTechnicalSuccess = ConfigurationManager.AppSettings["ThirdpartyTechnicalSuccessCodes"];
            foreach (string s in concatenatedTechnicalSuccess.Split(','))
            {
                technicalSuccessItems.Add(s.Trim(), s.Trim());
            }



        }
        ConcurrentDictionary<string, BankOneAgentObj> objDict = null;


        protected override async void BreakMessageAndFlush(string message)
        { try
            {
                Logger.Log("");
                Logger.Log(message);
                //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
                message = message.Trim('"', ' ');
                Logger.Log(message);
                float amount = 0;
                string[] inputParam = message.Split(',');
                //MTI, Date, AcquiringInstitutionID,MFBCode,Uniqueidentifier,ResponseCode

                float.TryParse(inputParam[5].Trim(), out amount);
                BankOneAgentObj obj = new BankOneAgentObj
                {
                    MTI = !string.IsNullOrEmpty(inputParam[0]) ? inputParam[0].Trim() : "NA",
                    TransactionType = !string.IsNullOrEmpty(inputParam[1]) ? inputParam[1].Trim() : "NA",
                    //Date = DateTime.ParseExact(inputParam[2].Trim(), "MM-dd-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    Date = DateTime.Parse(inputParam[2].Trim()),
                    Account1 = !string.IsNullOrEmpty(inputParam[3]) ? inputParam[3].Trim() : "NA",
                    Account2 = !string.IsNullOrEmpty(inputParam[4]) ? inputParam[4].Trim() : "NA",
                    Amount = amount,
                    UniqueIdentifier = !string.IsNullOrEmpty(inputParam[6]) ? inputParam[6].Trim() : "NA",
                    PayeeName = !string.IsNullOrEmpty(inputParam[7]) ? inputParam[7].Trim() : "NA",
                    Stan = !string.IsNullOrEmpty(inputParam[8]) ? inputParam[8].Trim() : "NA",
                    InstitutionCode = !string.IsNullOrEmpty(inputParam[9]) ? inputParam[9].Trim() : "NA",
                    ResponseCode = !string.IsNullOrEmpty(inputParam[10]) ? inputParam[10].Trim() : "NA",
                    Environment = !string.IsNullOrEmpty(inputParam[11]) ? inputParam[11].Trim() : "NA",

                };


                if (testInstitutionCodesToSkip.ContainsKey(obj.InstitutionCode))
                {
                    return;
                }

                Logger.Log("BankOneAgent Object {0},{1},{2},{3},{4} converted.", obj.UniqueIdentifier, obj.MTI, obj.Stan, obj.Date, obj.InstitutionCode);


                if (obj.MTI == "200")
                {
                    Logger.Log("Recording Request object with id: {0}", obj.UniqueIdentifier);

                    this.objDict.AddOrUpdate(obj.UniqueIdentifier, obj, (val1, val2) => obj);
                    return;
                }

                BankOneAgentObj initialObj = null;
                Logger.Log("Recording response object with id: {0}", obj.UniqueIdentifier);
                if (!this.objDict.TryRemove(obj.UniqueIdentifier, out initialObj))
                {
                    Logger.Log("Could not find response object with id: {0}", obj.UniqueIdentifier);
                    return;
                }


                Logger.Log("Sending response and request object to generate point to send to influx");
                var pointToWrite = GeneratePoint(obj, initialObj);
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
            catch(Exception ex)
            {
                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point GeneratePoint(BankOneAgentObj currentObj, BankOneAgentObj initialObj)
        {
            float FraudAmount = 0;
            string StringFraudAmount = ConfigurationManager.AppSettings["FraudAmount"];
            float.TryParse(StringFraudAmount, out FraudAmount);

            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "TRX", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    {"Environment", currentObj.Environment },
                    {"TransactionType", currentObj.TransactionType },
                    {"ResponseCode", currentObj.ResponseCode },
                    {"InstitutionName", InstitutionInfo.GetInstitutionName(currentObj.InstitutionCode,this.institutionsDict) },
                    {"InstitutionGroup" , GetGroup(currentObj.InstitutionCode, this.integrationFIs, this.BanKInTheBoxFIs) },
                    {"TechnicalSuccess", IsSuccessful(currentObj.ResponseCode) ? "Successful" : "Failed" },
                    { "FraudAmount", currentObj.Amount > FraudAmount ? "True" : "False" },

                },

                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    { "ResponseTime", currentObj.Date.Subtract(initialObj.Date).TotalMilliseconds },
                    { "Amount", currentObj.Amount},
                    { "FraudAmountCnt", currentObj.Amount > FraudAmount ? 1:0 },
                    { "Account1", currentObj.Account1},
                    { "Account2", currentObj.Account2},
                    { "PayeeName", currentObj.PayeeName},
                    { "UniqueId", currentObj.UniqueIdentifier },
                    { "InstitutionCode", currentObj.InstitutionCode },
                    { "TechnicalSuccessCnt", IsSuccessful(currentObj.ResponseCode) ? 1 : 0 },
                    { "TotalCnt", 1},
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

        private bool IsSuccessful(string responseCode)
        {
            if (!String.IsNullOrEmpty(responseCode) && this.technicalSuccessItems.ContainsKey(responseCode.Trim()))
            {
                return true;
            }
            return false;
        }


        private string GetGroup(string institutionCode, Dictionary<string, string> integrationInstitutions, Dictionary<string, string> BankInTheBoxMFBs)
        {
            if (integrationInstitutions.ContainsKey(institutionCode))
            {
                return "Integration FIs";
            }

            if (BankInTheBoxMFBs.ContainsKey(institutionCode))
            {
                return "BankInTheBox FIs";
            }

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

        public class BankOneAgentObj
        {
            public string MTI { get; set; }
            public string TransactionType { get; set; }
            public DateTime Date { get; set; }
            public string Account1 { get; set; }
            public string Account2 { get; set; }
            public string UniqueIdentifier { get; set; }
            public string PayeeName { get; set; }
            public string Stan { get; set; }
            public string InstitutionCode { get; set; }
            public string ResponseCode { get; set; }
            public float Amount { get; set; }
            public string Environment { get; set; }




        }


     
}
}
