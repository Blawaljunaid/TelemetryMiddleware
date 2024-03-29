﻿using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class QoreBIBTransactionProcessor : BaseProcessor
    {
        Dictionary<string, string> testInstitutionNamesToSkip = null;
        Dictionary<string, Institution> institutionsDict = null;
        Dictionary<string, string> technicalSuccesses = null;
        Dictionary<string, string> integrationFIs = null;
        Dictionary<string, string> BanKInTheBoxFIs = null;
        ConcurrentDictionary<string, QoreBIBTransactionObj> objDict = null;
        private readonly ConcurrentDictionary<string, QoreBIBTransactionObj> _entry = null;
        private readonly ConcurrentDictionary<string, QoreBIBTransactionObj> _apiCall = null;
        private readonly ConcurrentDictionary<string, QoreBIBTransactionObj> _postEntry = null;
        private readonly ConcurrentDictionary<string, QoreBIBTransactionObj> _onUsReqDict = null;
        private static readonly ConcurrentDictionary<string, string> Thresholds = new ConcurrentDictionary<string, string>();

        private readonly ConcurrentDictionary<string, QoreBIBTransactionObj> _transferObj = null;

        public QoreBIBTransactionProcessor(Loader loader, List<Institution> institutions) :
            base(loader, "QoreBIBTransactions", "__Monitoring.Qore.Transactions.BIB")
        {
            this.objDict = new ConcurrentDictionary<string, QoreBIBTransactionObj>();
            this._entry = new ConcurrentDictionary<string, QoreBIBTransactionObj>();
            this._apiCall = new ConcurrentDictionary<string, QoreBIBTransactionObj>();
            this._postEntry = new ConcurrentDictionary<string, QoreBIBTransactionObj>();
            this._onUsReqDict = new ConcurrentDictionary<string, QoreBIBTransactionObj>();
            this.institutionsDict = new Dictionary<string, Institution>();

            this._transferObj = new ConcurrentDictionary<string, QoreBIBTransactionObj>();
            //foreach (var institution in institutions)
            //{
            //    if (!this.institutionsDict.ContainsKey(institution.Code))
            //        this.institutionsDict.Add(institution.Code, institution);
            //}

            //this.integrationFIs = new Dictionary<string, string>();
            //string integrationFIsConcatenated = ConfigurationManager.AppSettings["IntegrationFIs"];
            //if (!String.IsNullOrEmpty(integrationFIsConcatenated))
            //{
            //    foreach (var s in integrationFIsConcatenated.Split(','))
            //    {
            //        integrationFIs.Add(s.Trim(), s.Trim());
            //    }
            //}

            //testInstitutionNamesToSkip = new Dictionary<string, string>();
            //string testInstitutionNames = ConfigurationManager.AppSettings["TestInstitutionNames"];
            //foreach (string s in testInstitutionNames.Split(','))
            //{
            //    testInstitutionNamesToSkip.Add(s.Trim(), s.Trim());
            //}

            //this.BanKInTheBoxFIs = new Dictionary<string, string>();
            //string BanKInTheBoxFIsConcatenated = ConfigurationManager.AppSettings["BanKInTheBoxFIs"];
            //if (!String.IsNullOrEmpty(BanKInTheBoxFIsConcatenated))
            //{
            //    foreach (var s in BanKInTheBoxFIsConcatenated.Split(','))
            //    {
            //        BanKInTheBoxFIs.Add(s.Trim(), s.Trim());
            //    }
            //}

            var appSettings = ConfigurationManager.AppSettings;


            Thresholds.TryAdd(ThresholdKeyRing.Airtime, appSettings.Get(ThresholdKeyRing.Airtime));
            Thresholds.TryAdd(ThresholdKeyRing.InterTransfer, appSettings.Get(ThresholdKeyRing.InterTransfer));
            Thresholds.TryAdd(ThresholdKeyRing.IntraTransfer, appSettings.Get(ThresholdKeyRing.IntraTransfer));
            Thresholds.TryAdd(ThresholdKeyRing.PayBills, appSettings.Get(ThresholdKeyRing.PayBills));
            Thresholds.TryAdd(ThresholdKeyRing.OnUsAirtime, appSettings.Get(ThresholdKeyRing.OnUsAirtime));
            Thresholds.TryAdd(ThresholdKeyRing.OnUsInterTransfer, appSettings.Get(ThresholdKeyRing.OnUsInterTransfer));
            Thresholds.TryAdd(ThresholdKeyRing.OnUsIntraTransfer, appSettings.Get(ThresholdKeyRing.OnUsIntraTransfer));
            Thresholds.TryAdd(ThresholdKeyRing.OnUsPayBills, appSettings.Get(ThresholdKeyRing.OnUsPayBills));


        }

        public class ThresholdKeyRing
        {
            public static string Airtime => "AirtimeRechargeThresholdSecs";
            public static string PayBills => "BillsPaymentTransferThresholdSecs";
            public static string IntraTransfer => "IntrabankTransferThresholdSecs";
            public static string InterTransfer => "InterbankTransferThresholdSecs";
            public static string OnUsAirtime => "OnUsAirtimeRechargeThresholdSecs";
            public static string OnUsPayBills => "OnUsBillsPaymentTransferThresholdSecs";
            public static string OnUsIntraTransfer => "OnUsIntrabankTransferThresholdSecs";
            public static string OnUsInterTransfer => "OnUsInterbankTransferThresholdSecs";
        }

        public class QoreBIBTransactionObj
        {
            public string Response { get; set; }
            public string TransactionDuration { get; set; }
            public string TransactionCategory { get; set; }
            public string Environment { get; set; }
            public string Client { get; set; }
            public string FailureCode { get; set; }
            public DateTime TransactionTime { get; set; }
            public string UniqueId { get; set; }
            public string TransactionLevel { get; set; }
            public string ProcessStatus { get; set; }
            public string CodeDescription { get; set; }
            public object CommandName { get; set; }
            public double? TimeDifference { get; set; }
            public string InstitutionName { get; set; }
        }

        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {
                Logger.Log("");
                Logger.Log(message);
                // sample message:  "15bce980-b198-4f37-b314-4678702eb5a9,Staging,BankOneInternetBanking,Successful,00,Intra Bank Transfer,13-01-2023 14:55:51:8476697 PM,2,ProvidusInternetBankingService.SameBankTransfer,Api Call Response"
                
                message = message.Trim('"', ' ');
                //Logger.Log(message);

                string[] inputParam = message.Split(',');

                QoreBIBTransactionObj obj = new QoreBIBTransactionObj
                {
                    UniqueId = inputParam[0].Trim(),
                    Environment = inputParam[1].Trim(),
                    Client = inputParam[2].Trim(),
                    Response = inputParam[3].Trim(),
                    FailureCode = inputParam[4].Trim(),
                    TransactionCategory = inputParam[5].Trim(),
                    TransactionTime = DateTime.ParseExact(inputParam[6].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                    TransactionLevel = inputParam[7].Trim(),
                    CommandName = inputParam[8].Trim(),
                    ProcessStatus = inputParam[9].Trim(),
                    InstitutionName = inputParam[10].Trim()
                };

                Logger.Log("QoreBIBTransaction Object {0},{1},{2},{3},{4},{5} converted.", obj.UniqueId, obj.Client, obj.Response, obj.TransactionCategory, obj.CommandName, obj.ProcessStatus);

                QoreBIBTransactionObj intialQoreBIBTransactionObj = null;
                Point processPointToWrite = null;

                switch (obj.ProcessStatus)
                {
                    case "Command Entry":
                        {
                            //Command Entry
                            Logger.Log($"Recording {obj.ProcessStatus} object with id {obj.UniqueId}");

                            switch (obj.TransactionCategory)
                            {
                                case "Transfer":
                                    {
                                        _transferObj.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                                        break;
                                    }
                                default:
                                    {
                                        _entry.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                                        break;
                                    }
                            }

                            break;
                        }
                    case "Api Call Request":
                        {
                            Logger.Log($"Recording {obj.ProcessStatus} object with id {obj.UniqueId}");
                            _apiCall.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);

                            if (!this._entry.TryGetValue(obj.UniqueId, out intialQoreBIBTransactionObj))
                            {
                                break;
                            }

                            Logger.Log($"Generating point for {obj.ProcessStatus} object with id {obj.UniqueId}");
                            processPointToWrite = GeneratePoint(obj, intialQoreBIBTransactionObj, "Pre Processing Duration");
                            Logger.Log($"Generating point for {obj.ProcessStatus} object with id {obj.UniqueId} complete");
                            Logger.Log($"Start writing to influx for {obj.ProcessStatus} object with id {obj.UniqueId}");
                            try
                            {
                                var preProcessing = await influxDbClient.WriteAsync(databaseName, processPointToWrite);
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
                            
                            Logger.Log($"DONE WRITING to influx for {obj.ProcessStatus} object with id {obj.UniqueId}");

                            break;
                        }
                    case "Api Call Response":
                        {
                            //Calculate the api call duration.
                            Logger.Log($"Recording {obj.ProcessStatus} object with id {obj.UniqueId}");
                            Logger.Log($"Calculating api call duration for the process with id {obj.UniqueId}");

                            if (!this._apiCall.TryGetValue(obj.UniqueId, out intialQoreBIBTransactionObj))
                            {
                                if (!this._transferObj.TryGetValue(obj.UniqueId, out intialQoreBIBTransactionObj))
                                {
                                    break;
                                }
                            }
                            #region On Us 
                            var time = obj.TransactionTime.Subtract(intialQoreBIBTransactionObj.TransactionTime).TotalMilliseconds;
                            var dObj = obj;
                            dObj.TimeDifference = time;
                            this._onUsReqDict.AddOrUpdate(dObj.UniqueId, dObj, (val1, val2) => dObj);
                            #endregion
                            _postEntry.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                            Logger.Log($"Generating point for {obj.ProcessStatus} object with id {obj.UniqueId}");
                            processPointToWrite = GeneratePoint(obj, intialQoreBIBTransactionObj, "Api Call Duration");
                            Logger.Log($"Generating point for {obj.ProcessStatus} object with id {obj.UniqueId} complete");
                            Logger.Log($"Start writing to influx for {obj.ProcessStatus} object with id {obj.UniqueId}");
                            try
                            {
                                var apiDu = await influxDbClient.WriteAsync(databaseName, processPointToWrite);
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
                            
                            Logger.Log($"DONE WRITING to influx for {obj.ProcessStatus} object with id {obj.UniqueId}");

                            if(obj.TransactionCategory == "Transfer" || obj.TransactionCategory == "Intra Bank Transfer" || obj.TransactionCategory == "Airtime Top Up")
                            {
                                //Calculate total duration
                                Logger.Log($"Recording {obj.ProcessStatus} object with id {obj.UniqueId}");
                                Logger.Log($"Calculating the Total duration for the process with id {obj.UniqueId}");
                                if (this._entry.TryRemove(obj.UniqueId, out intialQoreBIBTransactionObj))
                                {
                                    #region On Us
                                    if (this._onUsReqDict.ContainsKey(obj.UniqueId))
                                    {
                                        var time2 = obj.TransactionTime.Subtract(intialQoreBIBTransactionObj.TransactionTime).TotalMilliseconds;
                                        var dObj2 = this._onUsReqDict[obj.UniqueId];
                                        var timeDifference = time2 - dObj2.TimeDifference;
                                        Logger.Log($"Generating point in {obj.ProcessStatus} process for On Us Transactions object with id {obj.UniqueId}");
                                        var processPoint = GeneratePoint(obj, dObj, "On Us Transactions", timeDifference);
                                        Logger.Log($"Generating point for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId} complete");
                                        Logger.Log($"Start writing to influx for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId}");
                                        try
                                        {
                                            var difference = await influxDbClient.WriteAsync(databaseName, processPoint);
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
                                        
                                        Logger.Log($"DONE WRITING to influx for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId}");
                                    }
                                    #endregion
                                    Logger.Log($"Generating point for Total Duration for request with id {obj.UniqueId}");
                                    processPointToWrite = GeneratePoint(obj, intialQoreBIBTransactionObj, "Total Duration");
                                    Logger.Log($"Generating point for Total Duration for request with id {obj.UniqueId} complete");
                                    Logger.Log($"Start writing to influx for Total Duration for request with id {obj.UniqueId}");
                                    try
                                    {
                                        var totalDu = await influxDbClient.WriteAsync(databaseName, processPointToWrite);
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
                                    
                                    Logger.Log($"DONE WRITING to influx for Total Duration for request with id {obj.UniqueId}");
                                }else if(this._transferObj.TryGetValue(obj.UniqueId, out intialQoreBIBTransactionObj))
                                {
                                    #region On Us
                                    if (this._onUsReqDict.ContainsKey(obj.UniqueId))
                                    {
                                        var time2 = obj.TransactionTime.Subtract(intialQoreBIBTransactionObj.TransactionTime).TotalMilliseconds;
                                        var dObj2 = this._onUsReqDict[obj.UniqueId];
                                        var timeDifference = time2 - dObj2.TimeDifference;
                                        Logger.Log($"Generating point in {obj.ProcessStatus} process for On Us Transactions object with id {obj.UniqueId}");
                                        var processPoint = GeneratePoint(obj, dObj, "On Us Transactions", timeDifference);
                                        Logger.Log($"Generating point for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId} complete");
                                        Logger.Log($"Start writing to influx for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId}");

                                        try
                                        {
                                            var difference = await influxDbClient.WriteAsync(databaseName, processPoint);
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
                                        
                                        Logger.Log($"DONE WRITING to influx for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId}");
                                    }
                                    #endregion
                                    Logger.Log($"Generating point for Total Duration for request with id {obj.UniqueId}");
                                    processPointToWrite = GeneratePoint(obj, intialQoreBIBTransactionObj, "Total Duration");
                                    Logger.Log($"Generating point for Total Duration for request with id {obj.UniqueId} complete");
                                    Logger.Log($"Start writing to influx for Total Duration for request with id {obj.UniqueId}");
                                    try
                                    {
                                        var totalDu = await influxDbClient.WriteAsync(databaseName, processPointToWrite);
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
                                    
                                    Logger.Log($"DONE WRITING to influx for Total Duration for request with id {obj.UniqueId}");
                                }

                                //calculate post-processing duration
                                QoreBIBTransactionObj postEntry = null;
                                if (this._postEntry.TryRemove(obj.UniqueId, out postEntry))
                                {
                                    Logger.Log($"Generating point for Post Processing Duration for request with id {obj.UniqueId}");
                                    var pointToWrite = GeneratePoint(obj, postEntry, "Post Processing Duration");
                                    Logger.Log($"Generating point for Post Processing Duration for request with id {obj.UniqueId} complete");
                                    Logger.Log($"Start writing to influx for Post Processing Duration for request with id {obj.UniqueId}");
                                    try
                                    {
                                        var postProDu = await influxDbClient.WriteAsync(databaseName, pointToWrite);
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
                                    
                                    Logger.Log($"DONE WRITING to influx for Post Processing Duration for request with id {obj.UniqueId}");
                                }
                            }

                            break;
                        }
                    case "Command Exit":
                        {
                            //Calculate total duration
                            Logger.Log($"Recording {obj.ProcessStatus} object with id {obj.UniqueId}");
                            Logger.Log($"Calculating the Total duration for the process with id {obj.UniqueId}");
                            if (this._entry.TryRemove(obj.UniqueId, out intialQoreBIBTransactionObj) )
                            {
                                #region On Us
                                if (this._onUsReqDict.ContainsKey(obj.UniqueId))
                                {
                                    var time = obj.TransactionTime.Subtract(intialQoreBIBTransactionObj.TransactionTime).TotalMilliseconds;
                                    var dObj = this._onUsReqDict[obj.UniqueId];
                                    var timeDifference = time - dObj.TimeDifference;
                                    Logger.Log($"Generating point in {obj.ProcessStatus} process for On Us Transactions object with id {obj.UniqueId}");
                                    var processPoint = GeneratePoint(obj, dObj, "On Us Transactions", timeDifference);
                                    Logger.Log($"Generating point for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId} complete");
                                    Logger.Log($"Start writing to influx for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId}");
                                    try
                                    {
                                        var difference = await influxDbClient.WriteAsync(databaseName, processPoint);
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
                                    
                                    Logger.Log($"DONE WRITING to influx for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId}");
                                }
                                #endregion
                                Logger.Log($"Generating point for Total Duration for request with id {obj.UniqueId}");
                                processPointToWrite = GeneratePoint(obj, intialQoreBIBTransactionObj, "Total Duration");
                                Logger.Log($"Generating point for Total Duration for request with id {obj.UniqueId} complete");
                                Logger.Log($"Start writing to influx for Total Duration for request with id {obj.UniqueId}");
                                try
                                {
                                    var totalDu = await influxDbClient.WriteAsync(databaseName, processPointToWrite);
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
                                
                                Logger.Log($"DONE WRITING to influx for Total Duration for request with id {obj.UniqueId}");
                            }else if(this._transferObj.TryGetValue(obj.UniqueId, out intialQoreBIBTransactionObj))
                            {
                                #region On Us
                                if (this._onUsReqDict.ContainsKey(obj.UniqueId))
                                {
                                    var time = obj.TransactionTime.Subtract(intialQoreBIBTransactionObj.TransactionTime).TotalMilliseconds;
                                    var dObj = this._onUsReqDict[obj.UniqueId];
                                    var timeDifference = time - dObj.TimeDifference;
                                    Logger.Log($"Generating point in {obj.ProcessStatus} process for On Us Transactions object with id {obj.UniqueId}");
                                    var processPoint = GeneratePoint(obj, dObj, "On Us Transactions", timeDifference);
                                    Logger.Log($"Generating point for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId} complete");
                                    Logger.Log($"Start writing to influx for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId}");
                                    try
                                    {
                                        var difference = await influxDbClient.WriteAsync(databaseName, processPoint);
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
                                    
                                    Logger.Log($"DONE WRITING to influx for On Us Transactions {obj.ProcessStatus} object with id {obj.UniqueId}");
                                }
                                #endregion
                                Logger.Log($"Generating point for Total Duration for request with id {obj.UniqueId}");
                                processPointToWrite = GeneratePoint(obj, intialQoreBIBTransactionObj, "Total Duration");
                                Logger.Log($"Generating point for Total Duration for request with id {obj.UniqueId} complete");
                                Logger.Log($"Start writing to influx for Total Duration for request with id {obj.UniqueId}");
                                try
                                {
                                    var totalDu = await influxDbClient.WriteAsync(databaseName, processPointToWrite);
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
                                
                                Logger.Log($"DONE WRITING to influx for Total Duration for request with id {obj.UniqueId}");
                            }

                            //calculate post-processing duration
                            QoreBIBTransactionObj postEntry = null;
                            if (this._postEntry.TryRemove(obj.UniqueId, out postEntry))
                            {
                                Logger.Log($"Generating point for Post Processing Duration for request with id {obj.UniqueId}");
                                var pointToWrite = GeneratePoint(obj, postEntry, "Post Processing Duration");
                                Logger.Log($"Generating point for Post Processing Duration for request with id {obj.UniqueId} complete");
                                Logger.Log($"Start writing to influx for Post Processing Duration for request with id {obj.UniqueId}");
                                try
                                {
                                    var postProDu = await influxDbClient.WriteAsync(databaseName, pointToWrite);
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
                                
                                Logger.Log($"DONE WRITING to influx for Post Processing Duration for request with id {obj.UniqueId}");
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }
        
        private Point GeneratePoint(QoreBIBTransactionObj currentObj, QoreBIBTransactionObj initialObj, string scenario, double? timeDifference=null)
        {
            var time = timeDifference == null ? currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds : (double)timeDifference;
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = scenario, // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    {"InstitutionName", currentObj.InstitutionName },
                    {"FailureCode", currentObj.FailureCode},
                    {"Environment", currentObj.Environment},
                    {"Client", currentObj.Client},
                    {"Response", currentObj.Response},
                    {"TransactionCategory", currentObj.TransactionCategory},
                    {"CommandName", currentObj.CommandName},
                    {"Process", currentObj.ProcessStatus}
                },

                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    {"Time", time},
                    {"SuccessCnt", currentObj.Response == "Successful" ? 1 : 0},
                    {"TotalCnt", 1},
                    {"FailedCnt", currentObj.Response == "Failed" ? 1 : 0},
                    {"FailureNotOnUsCnt", currentObj.Response == "FailureNotOnUs" ? 1 : 0},
                    {"CodeDescription", currentObj.FailureCode}
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            if (scenario == "Total Duration")
            {
                var category = currentObj.TransactionCategory;

                int result = 0;
                switch (category)
                {
                    case "Transfer":
                        {
                            result = Calculate(time, ThresholdKeyRing.InterTransfer);
                            break;
                        }
                    case "Intra Bank Transfer":
                        {
                            result = Calculate(time, ThresholdKeyRing.IntraTransfer);
                            break;
                        }
                    case "Airtime Top Up":
                        {
                            result = Calculate(time, ThresholdKeyRing.Airtime);
                            break;
                        }
                    case "Providus Pay bill":
                        {
                            result = Calculate(time, ThresholdKeyRing.PayBills);
                            break;
                        }
                }

                //add threshold count.
                pointToWrite.Fields.Add("ThresholdCnt", result);

            }

            if (scenario == "On Us Transactions")
            {
                var OnUscategory = currentObj.TransactionCategory;

                int Onusresult = 0;
                switch (OnUscategory)
                {
                    case "Transfer":
                        {
                            Onusresult = Calculate(time, ThresholdKeyRing.OnUsInterTransfer);
                            break;
                        }
                    case "Intra Bank Transfer":
                        {
                            Onusresult = Calculate(time, ThresholdKeyRing.OnUsIntraTransfer);
                            break;
                        }
                    case "Airtime Top Up":
                        {
                            Onusresult = Calculate(time, ThresholdKeyRing.OnUsAirtime);
                            break;
                        }
                    case "Providus Pay bill":
                        {
                            Onusresult = Calculate(time, ThresholdKeyRing.OnUsPayBills);
                            break;
                        }
                }


                //add threshold count.
                pointToWrite.Fields.Add("OnUsThresholdCnt", Onusresult);
            }
            return pointToWrite;
        }

        public static int Calculate(double duration, string key)
        {
            Logger.Log($"In the CALCULATE function");

            int result = 0;
            string threshHold;
            if (Thresholds.TryGetValue(key, out threshHold))
            {
                var th = Convert.ToInt32(threshHold);
                th = th * 1000;
                result = duration > th ? 1 : 0;
            }
            return result;
        }

        //    private object GetInstitutionName(string institution)
        //    {
        //        Institution inst = null;
        //        if (!this.institutionsDict.TryGetValue(institution.Trim(), out inst))
        //        {
        //            inst = InstitutionInfo.GetInstitutionByCode(institution);
        //        }

        //        if (inst == null) inst = new Institution { Name = institution, Code = institution };

        //        return !String.IsNullOrEmpty(inst.Name) ? inst.Name : institution;
        //    }

        //    private string GetGroup(string institutionCode, Dictionary<string, string> integrationInstitutions, Dictionary<string, string> BankInTheBoxMFBs)
        //    {
        //        if (integrationInstitutions.ContainsKey(institutionCode))
        //        {
        //            return "Integration FIs";
        //        }

        //        if (BankInTheBoxMFBs.ContainsKey(institutionCode))
        //        {
        //            return "BankInTheBox FIs";
        //        }

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
    }
}
