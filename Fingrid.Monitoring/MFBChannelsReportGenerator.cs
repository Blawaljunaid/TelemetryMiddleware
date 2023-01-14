using Fingrid.Monitoring.Utility;
using InfluxDB.Net;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class MFBChannelsReportGenerator
    {
        protected string databaseName;
        protected InfluxDb influxDbClient = null;
        public MFBChannelsReportGenerator(Loader loader, string databaseName)
        {
            Logger.Log("MFBChannelsReportGenerator");
            this.influxDbClient = loader.InfluxDb;
            this.databaseName = databaseName;
            Logger.Log("databaseName");
        }

        public void GenerateReport()
        {
            GenerateDailyReport(DateTime.Today.AddDays(-2));
            GenerateDailyReport(DateTime.Today.AddDays(-1));
        }
        public async void GenerateDailyReport(DateTime dateToGenerate)
        {
            try
            {
                Logger.Log($"GenerateDailyReport:: About to run report for {dateToGenerate}");
                double dateFrom = dateToGenerate.Date.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                double dateTo = dateToGenerate.Date.AddDays(1).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                //validate records
                var validateItems = new InfluxDbHelper().QueryAPI(databaseName, $"Select * from ChannelTransactions2 where time >= {dateFrom}ms and time <= {dateTo}ms");

                if (validateItems?.Count > 0)
                {
                    Logger.Log($"GenerateDailyReport:: Report has already been generated.");
                    return;
                }

                Logger.Log($"GenerateDailyReport:: Report should now be generated.");
                //get  records
                var items = new InfluxDbHelper().QueryAPI(databaseName,
                    "Select sum(\"TotalCnt\") as \"TotalCnt\", sum(\"SuccessCnt\") as \"SuccessCnt\" from Trx1 "
                    + $"where time >= {dateFrom}ms and time <= {dateTo}ms GROUP BY \"InstitutionName\", "
                    + "\"ResponseCode\", \"TechnicalSuccess\", \"SwitchTransactionType\" fill(none)");

                Logger.Log($"GenerateDailyReport:: {items?.Count ?? 0} records retrieved");
                if (items == null) return;

                //convert records to Transaction counts
                var counts = new List<TransactionCount>();
                foreach (var item in items)
                {
                    var count = new TransactionCount
                    {
                        InstitutionName = item.Tags["InstitutionName"],
                        ResponseCode = item.Tags["ResponseCode"],
                        TechnicalSuccess = item.Tags["TechnicalSuccess"],
                        SwitchTransactionType = item.Tags["SwitchTransactionType"],
                        ResponseCount = (long)item.Values[0][1],
                        ResponseSuccessCount = (long)item.Values[0][2]
                    };

                    count.FailureCount = count.ResponseCount - count.ResponseSuccessCount;
                    counts.Add(count);
                }

                //Ensure all response Codes have records for all transaction types in an institution, to keep track of the total count
                var instDict = counts.GroupBy(c => c.InstitutionName);
                var techSuccessDict = new Dictionary<string, string>();
                counts.ForEach(c => techSuccessDict[c.ResponseCode] = c.TechnicalSuccess);

                foreach (var institution in instDict)
                {
                    var transResponlist = institution.Select(v => $"{v.ResponseCode}::##::{v.SwitchTransactionType}").Distinct().ToDictionary(c => c);
                    var disResponlist = institution.Select(v => v.ResponseCode).Distinct();
                    var disTranslist = institution.Select(v => v.SwitchTransactionType).Distinct();
                    string transResponItem = string.Empty;

                    foreach (var responseCode in disResponlist)
                    {
                        foreach (var transactionType in disTranslist)
                        {
                            if (!transResponlist.TryGetValue($"{responseCode}::##::{transactionType}", out transResponItem))
                            {
                                counts.Add(new TransactionCount
                                {
                                    InstitutionName = institution.Key,
                                    ResponseCode = responseCode,
                                    TechnicalSuccess = techSuccessDict[responseCode],
                                    SwitchTransactionType = transactionType,
                                    ResponseCount = 0,
                                    ResponseSuccessCount = 0
                                });
                            }
                        }
                    }
                }

                // Add total TransactionType count for all response Codes in an insitution
                var countsDict = counts.GroupBy(c => $"{c.InstitutionName}::##::{c.SwitchTransactionType}");
                foreach (var countItem in countsDict)
                {
                    long totalCount = countItem.Sum(x => x.ResponseCount);
                    long successCount = countItem.Sum(x => x.ResponseSuccessCount);

                    double successPercentage = (int)(successCount * 10000 / totalCount) * 0.01;

                    countItem.ToList().ForEach(c =>
                                    {
                                        c.TotalCount = totalCount;
                                        c.TotalSuccessCount = successCount;
                                        c.SuccessPercentage = successPercentage;
                                    });
                }

                //Generate InfluxDb data to be sent 
                List<Point> listpoints = new List<Point>();
                counts.ForEach(c =>
                {
                    listpoints.Add(new Point
                    {
                        Precision = InfluxDB.Net.Enums.TimeUnit.Milliseconds,
                        Measurement = "ChannelTransactions2", // serie/measurement/table to write into
                        Tags = new Dictionary<string, object>()
                        {
                        {"TechnicalSuccess", c.TechnicalSuccess },
                        {"InstitutionName", c.InstitutionName  },
                        {"ResponseCode", c.ResponseCode },
                        {"InstitutionTotalCount", c.TotalCount },
                        { "ResponseCount",  c.ResponseCount },
                        { "ResponseSuccessCount",  c.ResponseSuccessCount },
                        { "TotalSuccessCount",  c.TotalSuccessCount },
                        { "SuccessPercentage",  c.SuccessPercentage },
                        { "SwitchTransactionType", c.SwitchTransactionType }
                        },

                        Fields = new Dictionary<string, object>()
                        {
                        { "ResponseCount",  c.ResponseCount },
                        { "ResponseSuccessCount",  c.ResponseSuccessCount },
                        { "TotalSuccessCount",  c.TotalSuccessCount },
                        { "SuccessPercentage",  c.SuccessPercentage },
                        {"InstitutionTotalCount", c.TotalCount },
                        },
                        Timestamp = dateToGenerate.Date
                    });
                });

                Logger.Log($"GenerateDailyReport:: {listpoints?.Count ?? 0} records processed to be sent to influxDb");
                var query = await this.influxDbClient.WriteAsync("SwitchTransactions", listpoints.ToArray());
                Logger.Log($"GenerateDailyReport:: {listpoints?.Count ?? 0} records have been sent to innfluxDb");
            }
            catch (Exception ex)
            {
                Logger.Log($"GenerateDailyReport::Exception {ex.Message}|{ex.InnerException?.Message} at {ex.StackTrace}");
            }
        }
    }
}
