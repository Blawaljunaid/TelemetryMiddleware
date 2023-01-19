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
    public abstract class SwitchProcessor : BaseProcessor
    {
        Dictionary<string, string> channelAccessCodes = null;
        Dictionary<string, string> serviceCodesAndChannelAccess = null;
        Dictionary<string, string> integrationFIs = null;
        ConcurrentDictionary<string, SampleSwitchObj> objDict = null;
        long timeTakenThreshold = 0, requestTimeTakenThreshold = 0, responseTimeTakenThreshold = 0;

        string environment;
        string tableName = "";
        string tableName2 = "";
        Dictionary<string, string> technicalSuccessItems = null;
        Dictionary<string, string> testInstitutionCodesToSkip = null;
        Dictionary<string, Institution> institutionsDict = null;

        public SwitchProcessor(Loader loader, string channelName, string environmentName, List<Institution> institutions)
            : base(loader, "SwitchTransactions", channelName)
        {
            this.objDict = new ConcurrentDictionary<string, SampleSwitchObj>();
            this.tableName = "Trx1Gabetest";
            this.tableName2 = "Trx3Gabetest";
            technicalSuccessItems = new Dictionary<string, string>();



            this.institutionsDict = new Dictionary<string, Institution>();
            foreach (var institution in institutions)
            {
                if (!this.institutionsDict.ContainsKey(institution.Code))
                    this.institutionsDict.Add(institution.Code, institution);
            }

            string concatenatedTechnicalSuccess = ConfigurationManager.AppSettings["TechnicalSuccessCodes"];
            foreach (string s in concatenatedTechnicalSuccess.Split(','))
            {
                technicalSuccessItems.Add(s.Trim(), s.Trim());
            }

            testInstitutionCodesToSkip = new Dictionary<string, string>();
            string testInstitutionCodes = ConfigurationManager.AppSettings["TestInstitutionCodes"];
            foreach (string s in testInstitutionCodes.Split(','))
            {
                testInstitutionCodesToSkip.Add(s.Trim(), s.Trim());
            }

            this.environment = environmentName;


            //Kebbi Homes, Parallex, Advans, Gateway, Page            
            //Construct the Integration FIs. 
            this.integrationFIs = new Dictionary<string, string>();
            string integrationFIsConcatenated = ConfigurationManager.AppSettings["IntegrationFIs"];
            if (!String.IsNullOrEmpty(integrationFIsConcatenated))
            {
                foreach (var s in integrationFIsConcatenated.Split(','))
                {
                    integrationFIs.Add(s.Trim(), s.Trim());
                }
            }


            string channelAccessCodesFileName = ConfigurationManager.AppSettings["ChannelAccessCodesFileName"];
            if (!System.IO.File.Exists(channelAccessCodesFileName))
            {
                throw new Exception(String.Format("Channel Access File doesn't exist. Please check the location and try again. '{0}'", channelAccessCodesFileName));
            }
            string serviceCodesAndChannelAccessCodesFileName = ConfigurationManager.AppSettings["ServiceCodesAndChannelAccessCodesFileName"];
            if (!System.IO.File.Exists(serviceCodesAndChannelAccessCodesFileName))
            {
                throw new Exception(String.Format("ServiceCodes And Channel Access File doesn't exist. Please check the location and try again. '{0}'", serviceCodesAndChannelAccessCodesFileName));
            }


            channelAccessCodes = GetFileContentsToDictionary(channelAccessCodesFileName);
            this.serviceCodesAndChannelAccess = GetFileContentsToDictionary(serviceCodesAndChannelAccessCodesFileName);
            //Load all Channel Access Codes dictionaries 

            //load Threshold info 
            long.TryParse(ConfigurationManager.AppSettings["TimeTakenThreshold"], out timeTakenThreshold);
            long.TryParse(ConfigurationManager.AppSettings["RequestTimeTakenThreshold"], out requestTimeTakenThreshold);
            long.TryParse(ConfigurationManager.AppSettings["ResponseTimeTakenThreshold"], out responseTimeTakenThreshold);
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




        protected virtual string GetUniqueId(string[] inputParams)
        {
            //bool fromSwitch = Convert.ToBoolean(inputParams[3].Trim());
            //string MTI = inputParams[4].Trim();
            //string uniqueID;
            //if (!fromSwitch && MTI == "200")
            //{
            //    uniqueID = inputParams[12].Trim();//Strip out 200, 210 etc.
            //}
            //else uniqueID = inputParams[14].Trim();

            return $"{inputParams[7].Trim()}:{inputParams[8].Trim()}";
        }

        protected override async void BreakMessageAndFlush(string message)
        {
            Logger.Log("");
            Logger.Log(message);
            //message = message.Replace("TransactionTime", "SomethingElse").Trim('"', ' ');
            message = message.Trim('"', ' ');
            Logger.Log(message);

            string[] inputParam = message.Split(',');
            //,,,ServiceCode,channelAccessCode,RRN,STAN,MFBCODE,OriginalDataElements,ResponseCode
            SampleSwitchObj obj = new SampleSwitchObj
            {
                TransactionTime = DateTime.ParseExact(inputParam[0].Trim(), "dd-MM-yyyy HH:mm:ss:fffffff tt", System.Globalization.CultureInfo.InvariantCulture),
                Node = inputParam[1].Trim(),
                IsRequest = Convert.ToBoolean(inputParam[2].Trim()),
                FromSwitch = Convert.ToBoolean(inputParam[3].Trim()),
                MTI = inputParam[4].Trim(),
                ServiceCode = inputParam[5].Trim(),
                ChannelAccessCode = inputParam[6].Trim(),
                RetrievalReferenceNo = inputParam[7].Trim(),
                Stan = inputParam[8].Trim(),
                Institution = inputParam[9].Trim(),
                OriginalDataElements = inputParam[10].Trim(),
                ResponseCode = inputParam[11].Trim(),
                UniqueId = GetUniqueId(inputParam),
                SecondaryResponseCode = inputParam.Length > 16 ? inputParam[16].Trim() : string.Empty,
                AcquiringInstitution = inputParam.Length > 17 ? inputParam[17].Trim() : string.Empty,
                TerminalId = inputParam.Length > 18 ? (string.IsNullOrEmpty(inputParam[18].Trim()) ? string.Empty : inputParam[18].Trim().Substring(0, 1)) : string.Empty,
            };


            //Logger.Log("");
            //Logger.Log("Object {0},{1},{2},{3},{4} converted.", obj.UniqueId, obj.IsReponse, obj.PostingResponse, obj.PostingType, obj.TransactionTime);
            Logger.Log("Switch Object {0},{1},{2},{3},{4},{5},{6} converted.", obj.UniqueId, obj.TransactionTime, obj.FromSwitch, obj.IsRequest, obj.ResponseCode, obj.RetrievalReferenceNo, obj.MTI);
            if (testInstitutionCodesToSkip.ContainsKey(obj.Institution))
            {
                return;
            }


            if (obj.MTI == "420" || obj.MTI == "430")
            {
                ProcessReversals(influxDbClient, obj, databaseName);
            }

            if (obj.MTI == "200" || obj.MTI == "100" || obj.MTI == "600" || obj.MTI == "620" || obj.MTI == "120" || obj.MTI == "220" || obj.MTI == "320" || obj.MTI == "210" || obj.MTI == "110" || obj.MTI == "610" || obj.MTI == "630" || obj.MTI == "621" || obj.MTI == "130" || obj.MTI == "121" || obj.MTI == "230" || obj.MTI == "221" || obj.MTI == "330" || obj.MTI == "321")
            {
                ProcessRegularTransactions(influxDbClient, obj, databaseName);
            }
        }
        private async void ProcessReversals(InfluxDb influxDbClient, SampleSwitchObj obj, string databaseName)
        {
            string requestOrResponseString = "";
            string originatedFrom = "";
            if (!obj.FromSwitch && (obj.MTI == "420"))
            {
                requestOrResponseString = "1";
                originatedFrom = "External";
            }
            if (obj.FromSwitch && (obj.MTI == "430"))
            {
                requestOrResponseString = "2";
                originatedFrom = "External";
            }

            if (obj.FromSwitch && (obj.MTI == "420"))
            {
                requestOrResponseString = "1";
                originatedFrom = "Internal";
            }
            if (!obj.FromSwitch && (obj.MTI == "430"))
            {
                requestOrResponseString = "2";
                originatedFrom = "Internal";
            }

            if (!String.IsNullOrEmpty(requestOrResponseString))
            {

                var requestOrResponsePoint = GenerateRequestOrResponsePoint(obj, requestOrResponseString, "ReversalTrx2Gabetest", originatedFrom);
                Logger.Log("From ProcessReversals method, We want to write this to influx: " + Newtonsoft.Json.JsonConvert.SerializeObject(requestOrResponsePoint));

                try
                {
                    //Logger.Log("Influx DB is {0}null. Point is {1}null.", (influxDbClient == null ? "" : "NOT "), (pointToWrite == null ? "" : "NOT "));
                    var response2 = await influxDbClient.WriteAsync(databaseName, requestOrResponsePoint);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error on ProcessReversals when writing to influx db: " + ex.Message);
                    //Logger.Log(Newtonsoft.Json.JsonConvert.SerializeObject(requestOrResponsePoint));
                }
                //Logger.Log("From ProcessReversals, I HAVE WRITTEN THIS TO INFLUX: " + Newtonsoft.Json.JsonConvert.SerializeObject(requestOrResponsePoint));
            }
        }
        private async Task SendDefaultResponseAsync()
        {
            bool exhausted = false;
            while (!exhausted)
            {
                var requestList = this.objDict.Where(c => c.Value.IsRequest == true);
                var responseList = this.objDict.Where(c => c.Value.IsRequest == false);
                if (requestList.ToList().Count == 0)
                {
                    exhausted = true;
                    return;
                }
                foreach (var request in requestList)
                {
                    if (!responseList.Any(c => c.Value.UniqueId == request.Value.UniqueId) && DateTime.Now.Subtract(request.Value.RequestEntryTime).TotalMinutes >= 5)
                    {
                        var defaultResponse = new SampleSwitchObj()
                        {
                            IsRequest = false,
                            FromSwitch = true,
                            Node = "defaultSwitchNode",
                            MTI = "210",
                            ResponseCode = "No response",
                            Institution = request.Value.Institution,
                            Stan = request.Value.Stan,
                            OriginalDataElements = request.Value.OriginalDataElements,
                            RetrievalReferenceNo = request.Value.RetrievalReferenceNo,
                            ChannelAccessCode = request.Value.ChannelAccessCode,
                            ServiceCode = request.Value.ServiceCode,
                            TransactionTime = DateTime.Now,
                            UniqueId = request.Value.UniqueId,
                            SecondaryResponseCode = "No response",
                            AcquiringInstitution = request.Value.AcquiringInstitution,


                        };
                        exhausted = await InfluxTimeDifferenceAsync(request.Value, defaultResponse);
                    }
                }
            }
        }
        private async void ProcessRegularTransactions(InfluxDb influxDbClient, SampleSwitchObj obj, string databaseName)
        {
            string requestOrResponseString = "";

            if (!obj.FromSwitch && (obj.MTI == "200" || obj.MTI == "100" || obj.MTI == "600" || obj.MTI == "620" || obj.MTI == "120" || obj.MTI == "220" || obj.MTI == "320"))
            {
                requestOrResponseString = "1";
            }
            if (obj.FromSwitch && (obj.MTI == "210" || obj.MTI == "110" || obj.MTI == "610" || obj.MTI == "630" || obj.MTI == "621" || obj.MTI == "130" || obj.MTI == "121" || obj.MTI == "230" || obj.MTI == "221" || obj.MTI == "330" || obj.MTI == "321"))
            {
                requestOrResponseString = "2";
            }

            if (!String.IsNullOrEmpty(requestOrResponseString))
            {
                var requestOrResponsePoint = GenerateRequestOrResponsePoint(obj, requestOrResponseString, "Trx2Gabetest", "");

                try
                {
                    var response2 = await influxDbClient.WriteAsync(databaseName, requestOrResponsePoint);
                }
                catch (Exception ex)
                {
                    Logger.Log("ERROR: From ProcessRegularTransactions method, Trying to send this: " + Newtonsoft.Json.JsonConvert.SerializeObject(requestOrResponsePoint));
                    Logger.Log("Error Generating a point in GeneratePoint method to send to influx db: " + ex.Message);
                    //Logger.Log(ex.Message);
                }

                //Logger.Log("From ProcessRegularTransactions method, I HAVE WRITTEN THIS TO INFLUX: " + Newtonsoft.Json.JsonConvert.SerializeObject(requestOrResponsePoint));


            }

            if (!obj.FromSwitch && (obj.MTI == "200" || obj.MTI == "100" || obj.MTI == "600" || obj.MTI == "620" || obj.MTI == "120" || obj.MTI == "220" || obj.MTI == "320"))
            {
                obj.RequestEntryTime = DateTime.Now;
                this.objDict.AddOrUpdate(obj.UniqueId, obj, (val1, val2) => obj);
                //await Task.Run(() => SendDefaultResponseAsync());
                return;
            }

            if (obj.FromSwitch && (obj.MTI == "210" || obj.MTI == "110" || obj.MTI == "610" || obj.MTI == "630" || obj.MTI == "621" || obj.MTI == "130" || obj.MTI == "121" || obj.MTI == "230" || obj.MTI == "221" || obj.MTI == "330" || obj.MTI == "321")) { }
            else
            {
                SampleSwitchObj existingRequestObj = null;
                //SampleSwitchObj secondObj = null;

                //Retrieve.
                if (!this.objDict.TryGetValue(obj.UniqueId, out existingRequestObj))
                {
                    return;
                }
                //Set Request Time

                bool isFepNode = obj.Node?.ToUpper().Contains(ConfigurationManager.AppSettings["FepNodeName"]) ?? false;
                if (obj.FromSwitch && (obj.MTI == "200" || obj.MTI == "100" || obj.MTI == "600" || obj.MTI == "620" || obj.MTI == "120" || obj.MTI == "220" || obj.MTI == "320") && !isFepNode)
                {
                    existingRequestObj.RequestDuration = obj.TransactionTime.Subtract(existingRequestObj.TransactionTime).TotalMilliseconds;
                    existingRequestObj.ServiceCode = obj.ServiceCode;
                    existingRequestObj.Institution = obj.Institution;
                    existingRequestObj.ResponseDuration = -1;
                    existingRequestObj.RequestEndTime = obj.TransactionTime;
                }
                //Get Response Start TIme
                if (!obj.FromSwitch && (obj.MTI == "210" || obj.MTI == "110" || obj.MTI == "610" || obj.MTI == "630" || obj.MTI == "621" || obj.MTI == "130" || obj.MTI == "121" || obj.MTI == "230" || obj.MTI == "221" || obj.MTI == "330" || obj.MTI == "321") && !isFepNode)
                {
                    existingRequestObj.ResponseStartTime = obj.TransactionTime;
                }

                if (obj.FromSwitch && (obj.MTI == "200" || obj.MTI == "100" || obj.MTI == "600" || obj.MTI == "620" || obj.MTI == "120" || obj.MTI == "220" || obj.MTI == "320") && isFepNode)
                {

                    existingRequestObj.AcquiringInstitution = obj.AcquiringInstitution;

                }

                return;
            }

            SampleSwitchObj initialRequestObj = null;
            //SampleSwitchObj secondObj = null;

            //Remove.
            if (!this.objDict.TryRemove(obj.UniqueId, out initialRequestObj))
            {
                return;
            }

            await InfluxTimeDifferenceAsync(obj, initialRequestObj);

        }

        private async Task<bool> InfluxTimeDifferenceAsync(SampleSwitchObj obj, SampleSwitchObj initialRequestObj)
        {
            var pointToWrite = GeneratePoint(obj, initialRequestObj);
            var pointToWrite2 = GenerateSettlementAcquirerPoint(obj, initialRequestObj);
            //Point is then passed into Client.WriteAsync method together with the database name:

            try
            {
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                var response2 = await influxDbClient.WriteAsync(databaseName, pointToWrite2);
                return true;
            }
            catch (Exception e)
            {
                Trace.TraceError($"Stack Trace:{e.StackTrace}, Inner Exception:{e.InnerException?.Message}, Message:{e.Message}");
                Logger.Log("Error in InfluxTimeDifferenceAsync method to send to influx db: " + e.Message);
                return false;
            }

            Logger.Log("I HAVE PUSHED InfluxTimeDifferenceAsync method TO INFLUX");


        }
        private Object thisLock = new Object();

        public void Withdraw(decimal amount)
        {
            lock (thisLock)
            {
            }
        }


        private Point GenerateRequestOrResponsePoint(SampleSwitchObj currentObj, string requestOrResponseString, string measurement, string originatedFrom)
        {
            var secondaryResponseCode = currentObj.SecondaryResponseCode;
            var terminalId = currentObj.TerminalId;
            var responsecode = currentObj.ResponseCode;
            if (string.IsNullOrEmpty(secondaryResponseCode)) secondaryResponseCode = "N/A";
            if (string.IsNullOrEmpty(terminalId)) terminalId = "N/A";
            if (string.IsNullOrEmpty(responsecode)) responsecode = "N/A";

            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = measurement, // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                        //{ "Server", obj.Server },
                    {"Environment" ,  this.environment },
                    {"Institution",string.IsNullOrEmpty(currentObj.Institution) ? "N/A" : currentObj.Institution},
                    {"InstitutionName", GetInstitutionName(currentObj.Institution) },
                    //{"CRMName", GetCRMName(currentObj.Institution) },
                    {"ResponseCode", responsecode},
                    {"TechnicalSuccess", IsSuccessful(currentObj.ResponseCode) ? "Successful" : "Failed" },
                    {"SwitchInstitutionGroup" , GetGroup(currentObj.Institution, this.integrationFIs) },
                    {"SwitchTransactionType",  GetTransactionType(currentObj.ServiceCode)},
                    {"IsRequest", requestOrResponseString},
                    {"SecondaryResponseCode", secondaryResponseCode},
                    {"TerminalId", terminalId},

                },

                Fields = new Dictionary<string, object>()
                {
                    { "UniqueId",  currentObj.UniqueId},
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            if (!String.IsNullOrEmpty(originatedFrom))
            {
                pointToWrite.Tags.Add("OriginatedFrom", originatedFrom);
            }

            return pointToWrite;
        }


        private Point GeneratePoint(SampleSwitchObj currentObj, SampleSwitchObj initialObj)
        {
            double totalMillisecs = currentObj.TransactionTime.Subtract(initialObj.TransactionTime).TotalMilliseconds;
            if (initialObj.ResponseStartTime.HasValue) initialObj.ResponseDuration = currentObj.TransactionTime.Subtract(initialObj.ResponseStartTime.Value).TotalMilliseconds;
            var secondaryResponseCode = string.Empty;
            var responsecode = currentObj.ResponseCode;
            if (string.IsNullOrEmpty(secondaryResponseCode)) secondaryResponseCode = currentObj.SecondaryResponseCode;
            if (string.IsNullOrEmpty(secondaryResponseCode)) secondaryResponseCode = initialObj.SecondaryResponseCode;
            if (string.IsNullOrEmpty(secondaryResponseCode)) secondaryResponseCode = "N/A";
            if (string.IsNullOrEmpty(responsecode)) responsecode = "N/A";

            double hourOfDay = DateTime.Now.Subtract(DateTime.Today).TotalHours;
            bool isDayShift = hourOfDay >= 8 && hourOfDay <= 21;
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = this.tableName, // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    {"Environment" ,  this.environment },
                    {"ResponseCode", responsecode },
                    {"InstitutionName", GetInstitutionName(currentObj.Institution) },
                    {"CRMName", GetCRMName(currentObj.Institution) },
                    {"Institution",string.IsNullOrEmpty(currentObj.Institution) ? "N/A" : currentObj.Institution },
                    {"TechnicalSuccess", IsSuccessful(currentObj.ResponseCode) ? "Successful" : "Failed" },
                    {"SwitchInstitutionGroup" , GetGroup(currentObj.Institution, this.integrationFIs) },
                    {"SwitchTransactionType",  GetTransactionType(initialObj.ServiceCode)},
                    {"ServiceCode",  string.IsNullOrEmpty(currentObj.ServiceCode) ? "N/A" : initialObj.ServiceCode},
                    {"SecondaryResponseCode", secondaryResponseCode},
                    {"Shift", isDayShift? "DayShift" : "NightShift"},
                    {"TerminalId", string.IsNullOrEmpty(currentObj.TerminalId) ? "N/A" : currentObj.TerminalId},
                    //{"AcquiringInstitutionName", GetInstitutionName(currentObj.AcquiringInstitution) },
                    //{"AcquiringInstitution", currentObj.AcquiringInstitution },
                },

                Fields = new Dictionary<string, object>()
                {
                    {"TimeTaken",  totalMillisecs},
                    {"RequestTimeTaken",  initialObj.RequestDuration},
                    {"ResponseTimeTaken",  initialObj.ResponseDuration},
                    {"ResponseStartTime",  initialObj.ResponseStartTime.HasValue? initialObj.ResponseStartTime.Value.ToString() : "Not Set"},
                    {"RequestEndTime",   initialObj.RequestEndTime.HasValue? initialObj.RequestEndTime.Value.ToString() : "Not Set"},
                    {"RequestStartTime",  initialObj.TransactionTime.ToString()},
                    {"UniqueId",  currentObj.UniqueId},
                    {"InstitutionCode", currentObj.Institution },
                    //{"AcquiringInstitution", currentObj.AcquiringInstitution },
                    { "Time", totalMillisecs },
                    { "SuccessCnt", IsSuccessful(currentObj.ResponseCode) ? 1 : 0 },
                    {"TotalCnt", 1},
                    {"TraxDurationthreshold", (totalMillisecs > timeTakenThreshold) ? 1 : 0},
                    {"RequestDurationthreshold", (initialObj.RequestDuration > requestTimeTakenThreshold ) ? 1 : 0},
                    {"ResponseDurationthreshold", (initialObj.ResponseDuration > responseTimeTakenThreshold) ? 1 : 0},
                    

                     //{ "Time2", currentObj.TransactionTime.Subtract(secondObj.TransactionTime).Milliseconds },
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


        private Point GenerateSettlementAcquirerPoint(SampleSwitchObj currentObj, SampleSwitchObj initialObj)
        {
            var secondaryResponseCode = string.Empty;
            var responsecode = currentObj.ResponseCode;
            if (string.IsNullOrEmpty(secondaryResponseCode)) secondaryResponseCode = currentObj.SecondaryResponseCode;
            if (string.IsNullOrEmpty(secondaryResponseCode)) secondaryResponseCode = initialObj.SecondaryResponseCode;
            if (string.IsNullOrEmpty(secondaryResponseCode)) secondaryResponseCode = "N/A";
            if (string.IsNullOrEmpty(responsecode)) responsecode = "N/A";

            double hourOfDay = DateTime.Now.Subtract(DateTime.Today).TotalHours;
            bool isDayShift = hourOfDay >= 8 && hourOfDay <= 21;
            var pointToWrite2 = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = this.tableName2, // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {
                    {"Environment" ,  this.environment },
                    {"ResponseCode", responsecode},
                    {"InstitutionName", GetInstitutionName(currentObj.Institution) },
                    {"CRMName", GetCRMName(currentObj.Institution) },
                    {"Institution", string.IsNullOrEmpty(currentObj.Institution) ? "N/A" : currentObj.Institution },
                    {"TechnicalSuccess", IsSuccessful(currentObj.ResponseCode) ? "Successful" : "Failed" },
                    {"SwitchInstitutionGroup" , GetGroup(currentObj.Institution, this.integrationFIs) },
                    {"SwitchTransactionType",  GetTransactionType(initialObj.ServiceCode)},
                    {"ServiceCode",string.IsNullOrEmpty(currentObj.ServiceCode) ? "N/A" : initialObj.ServiceCode},
                    {"SecondaryResponseCode", secondaryResponseCode},
                    {"Shift", isDayShift? "DayShift" : "NightShift"},
                    {"AcquiringInstitutionName", string.IsNullOrEmpty(currentObj.AcquiringInstitution)? "N/A": GetInstitutionName(currentObj.AcquiringInstitution) ?? "N/A" },
                    {"AcquiringInstitution", string.IsNullOrEmpty(currentObj.AcquiringInstitution)? "N/A": currentObj.AcquiringInstitution },
                    {"TerminalId", string.IsNullOrEmpty(currentObj.TerminalId)? "N/A": currentObj.TerminalId},
                },

                Fields = new Dictionary<string, object>()
                {
                    {"UniqueId",  currentObj.UniqueId},
                    {"InstitutionCode", currentObj.Institution },
                    {"AcquiringInstitution", currentObj.AcquiringInstitution },
                    { "SuccessCnt", IsSuccessful(currentObj.ResponseCode) ? 1 : 0 },
                    {"TotalCnt", 1},

                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite2;
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
            return !String.IsNullOrEmpty(inst.Name) ? inst.Name : institution;
        }

        private object GetCRMName(string institution)
        {
            Crm theCRM = null;
            theCRM = CrmInfo.GetCrmByInstitutionCode(institution);

            if (theCRM == null) theCRM = new Crm { CRM = institution, InstitutionCode = institution };

            if (string.IsNullOrEmpty(institution)) institution = "N/A";
            return !String.IsNullOrEmpty(theCRM.CRM) ? theCRM.CRM : institution;
        }

        private bool IsSuccessful(string responseCode)
        {
            if (!String.IsNullOrEmpty(responseCode) && this.technicalSuccessItems.ContainsKey(responseCode.Trim()))
            {
                return true;
            }
            return false;
        }



        private string GetTransactionType(string serviceCode)
        {
            string transactionTypeName = "";

            if (!String.IsNullOrEmpty(serviceCode) && serviceCode.Contains("a"))
            {
                serviceCode = serviceCode.Split('a')[0];
            }

            string channelAccess = "";
            if (this.serviceCodesAndChannelAccess.TryGetValue(serviceCode, out channelAccess))
            {
                if (this.channelAccessCodes.TryGetValue(channelAccess, out transactionTypeName))
                {
                    return transactionTypeName;
                }
            }

            if (String.IsNullOrEmpty(transactionTypeName))
            {
                transactionTypeName = "Others";
            }

            return transactionTypeName;
        }

        private string GetGroup(string institutionCode, Dictionary<string, string> integrationInstitutions)
        {
            if (integrationInstitutions.ContainsKey(institutionCode))
            {
                return "Integration FIs";
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

        public class SampleSwitchObj
        {
            //public string TransactionTime { get; set; }
            public bool IsRequest { get; set; }
            public bool FromSwitch { get; set; }

            public string Node { get; set; }
            public string MTI { get; set; }
            public string ResponseCode { get; set; }
            public string Institution { get; set; }
            public string Stan { get; set; }
            public string OriginalDataElements { get; set; }
            public string RetrievalReferenceNo { get; set; }
            public string ChannelAccessCode { get; set; }
            public string ServiceCode { get; set; }
            public DateTime TransactionTime { get; set; }
            public string UniqueId { get; set; }
            public double RequestDuration { get; set; }
            public double ResponseDuration { get; set; }
            public DateTime? ResponseStartTime { get; set; }
            public DateTime? RequestEndTime { get; set; }
            public string SecondaryResponseCode { get; set; }
            public string AcquiringInstitution { get; set; }
            public DateTime RequestEntryTime { get; set; }
            public string TerminalId { get; set; }

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

