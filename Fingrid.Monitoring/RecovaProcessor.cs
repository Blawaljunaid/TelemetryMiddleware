using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using Jil;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Fingrid.Monitoring.BankoneWebApi;
using static Fingrid.Monitoring.BaseIBWebClientFlow;

namespace Fingrid.Monitoring
{
    public class RecovaProcessor : BaseProcessor
    {
        ConcurrentDictionary<string, RecovaProcessorObj> objDict = null;

        public RecovaProcessor(Loader loader)
           : base(loader, "RecovaProcessor", "recova_middleware_transactions-staging")
        {
            this.objDict = new ConcurrentDictionary<string, RecovaProcessorObj>();

        }
        public class RecovaProcessorObj
        {
            public string RequestId { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
            public string InstitutionCode { get; set; }
            public string InstitutionName { get; set; }
            public double Amount { get; set; }
            public string TransactionType { get; set; }
            public string Status { get; set; }
            public DateTime CreateDate { get; set; }
        }

        protected override async void BreakMessageAndFlush(string message)
        {
            try
            {


                Logger.Log("");
                Logger.Log(message);
                // sample message:  "GetAccountByAccountNumber,8d44f45d-d12e-4521-bb12-28e24b8c5ae2,true,20-01-2023 11:38:12:5743448 AM,Failed,No savings/current account was found with the account number: 00510012000003282,100614,dev"
                //message = message.Trim('"', ' ');
                //message = message.Replace('"', '\"');
                //Logger.Log(message);
                //string[] inputParam = message.Split(',');

                RecovaProcessorObj obj = JsonConvert.DeserializeObject<RecovaProcessorObj>(message);


                Logger.Log("RecovaProcessor Object {0},{1},{2},{3},{4},{5} converted.", obj.RequestId, obj.ResponseCode, obj.TransactionType, obj.Status, obj.InstitutionName, obj.ResponseMessage);

                Console.WriteLine("Sending object to generate point to send to influx");
                var pointToWrite = GeneratePoints(obj);
                Console.WriteLine("Generate point complete");


                Console.WriteLine("Start writing to influx");
                var response = await influxDbClient.WriteAsync(databaseName, pointToWrite);
                Console.WriteLine("DONE writing to influx");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                Logger.Log(ex.InnerException?.Message);
                Logger.Log(ex.StackTrace);
            }

        }

        private Point GeneratePoints(RecovaProcessorObj currentObj)
        {
            var pointToWrite = new Point()
            {
                Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                Measurement = "Trx",
                Tags = new Dictionary<string, object>()
                {

                    {"ResponseCode", currentObj.ResponseCode },
                    {"InstitutionName", currentObj.InstitutionName },
                    {"TransactionType", currentObj.TransactionType },
                    {"Status", currentObj.Status },

                },
                Fields = new Dictionary<string, object>()
                {
                    //{"Time", currentObj.CreateDate.Subtract(initialObj.Time).TotalMilliseconds },
                    {"Time", currentObj.CreateDate.Millisecond },
                    {"UniqueId", currentObj.RequestId },
                    {"Amount", currentObj.Amount },
                    {"ResponseMessage", currentObj.ResponseMessage },
                    {"UniqueId", currentObj.RequestId },
                },
                Timestamp = DateTime.UtcNow // optional (can be set to any DateTime moment)
            };

            return pointToWrite;
        }


    }
}
