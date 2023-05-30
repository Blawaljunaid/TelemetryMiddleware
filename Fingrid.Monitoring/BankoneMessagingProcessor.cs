using InfluxDB.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fingrid.Monitoring.Utility;

namespace Fingrid.Monitoring
{
   
    public class BankoneMessagingProcessor : BaseProcessor
    {
        
        public BankoneMessagingProcessor(Loader loader)
            : base(loader, "CriticalServicesMonitor", "BankOne.MessagingService.Live")
        {

            Logger.Log("BankoneMessagingProcessor");

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

                string[] inputParam = message.Split(',');
                if (inputParam.Length != 7)
                {
                    Logger.Log("Returning... Reason: Message string not equal to 7");
                    return;
                }
                //MTI, Date, AcquiringInstitutionID,MFBCode,Uniqueidentifier,ResponseCode
                BankoneMessagingProcessorObj obj = new BankoneMessagingProcessorObj
                {
                    Institution = inputParam[0].Trim(),
                    UniqueId = inputParam[1].Trim(),
                
                    IsResponse = inputParam[3].Trim() == "1",
                    SmsProviderOption = inputParam[4].Trim(),
                    Stage = inputParam[5].Trim(),
                    StatusResponse = inputParam[6].Trim(),
                

                };
                Logger.Log("BankoneMessagingProcessor Object {0},{1},{2},{3},{4},{5} converted.", obj.Institution, obj.UniqueId, obj.IsResponse, obj.SmsProviderOption, obj.Stage, obj.StatusResponse);

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

        private Point GeneratePoint(BankoneMessagingProcessorObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "BankOneMessaging", // serie/measurement/table to write into
                Tags = new Dictionary<string, object>()
                {

                    {"Institution", currentObj.Institution },
                    {"Stage", currentObj.Stage },
                    {"StatusResponse", currentObj.StatusResponse },
                    {"IsResponse", currentObj.IsResponse ? "Response" : "Request" },
                    
                    {"SmsProviderOption", currentObj.SmsProviderOption },

                },

                Fields = new Dictionary<string, object>()
                {
                    //{ "Time", DateTime.Parse(obj.TransactionTime).Subtract(DateTime.Parse (initialRequestObj.TransactionTime)).Milliseconds },
                    
                    {"UniqueId", currentObj.UniqueId },
                    {"TotalCnt", 1},
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }

       

        public class BankoneMessagingProcessorObj
        {
            
            public string Stage { get; set; }
            public bool IsResponse { get; set; }
            public string Institution { get; set; }
            public string UniqueId { get; set; }
            public string StatusResponse { get; set; }
            public string SmsProviderOption { get; set; }
            



        }


    }
}
