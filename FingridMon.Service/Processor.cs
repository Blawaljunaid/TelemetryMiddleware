using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fingrid.Monitoring;
using Fingrid.Monitoring.Utility;
using System.Collections;
using System.Configuration;
using System.Net.Http;
using System.Threading;
using System.Timers;
using Timer = System.Threading.Timer;

namespace FingridMon.Service
{
    public class Processor : BackgroundService
    {
        DateTime lastMfbChannelsReportRunTime;
        private System.Timers.Timer timer = null;
        private bool isBusy = false;
        Fingrid.Monitoring.Loader loader = new Fingrid.Monitoring.Loader();
        List<Fingrid.Monitoring.BaseProcessor> processors = new List<Fingrid.Monitoring.BaseProcessor>();
        private static string publisherUrl = System.Configuration.ConfigurationManager.AppSettings["PublisherUrl"];
        private static bool shouldPublishToRedis = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["shouldPublishToRedis"]);

        private readonly ILogger<Processor> _logger;

        public Processor(ILogger<Processor> logger)
        {
            _logger = logger;
        }

        public void StartUpEngine()
        {
            System.Threading.Thread thWorker = new System.Threading.Thread(new System.Threading.ThreadStart(

               delegate
               {
                   try
                   {
                       isBusy = true;
                       var institutions = LoadInstitutions();


                       loader.LoadAll();
                       System.Threading.ThreadPool.SetMinThreads(2, 10); // newly added

                       processors.Add(new Fingrid.Monitoring.GoGridCentralSwitchProcessor(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.CoreBankingSwitchProcessor(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.UssdProcessor(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.CbaProcessor(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.EODCoordinatorProcessor(loader));
                       //processors.Add(new Fingrid.Monitoring.CreditClubUptime(loader));
                       //processors.Add(new Fingrid.Monitoring.BankOneMobileUptime(loader));
                       //processors.Add(new Fingrid.Monitoring.BankoneMessagingProcessor(loader));
                       //processors.Add(new Fingrid.Monitoring.ExternalThirdParty(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BankOneAgent(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BankOneCacheDuration(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.MyBankOneRequestDuration(loader));
                       //processors.Add(new Fingrid.Monitoring.NibbsInward(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BankOneWebAPIMonitor(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.CreditClubMobileTracker(loader));
                       //processors.Add(new Fingrid.Monitoring.BankOneMobileTracker(loader));

                       //processors.Add(new Fingrid.Monitoring.iPNXSwitchProcessor(loader, institutions)); // not in use

                       //// processors.Add(new Fingrid.Monitoring.CbaIsoProcessor(loader, institutions)); // Crashing with index out of bound
                       //// processors.Add(new Fingrid.Monitoring.CbaIsoProcessor2(loader, institutions));  // Crashing with index out of bound

                       //processors.Add(new Fingrid.Monitoring.BankOneAnniversaryNotificationProcessor(loader));
                       //processors.Add(new Fingrid.Monitoring.BatchAccountStatementGeneration(loader));
                       //processors.Add(new Fingrid.Monitoring.PreConfirmationProcessor(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.USSDSimulatorMonitoring(loader));
                       //processors.Add(new Fingrid.Monitoring.InternetBankingProcessor(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.InternalThirdParty(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.CorrespondentBk_App(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.CorrespondentBk_Web(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BankOneBranchlessBankingClientFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BranchlessBankingServerFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BankOneMobileClientFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BankOneMobileServerFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.InternetBankingMobileClientFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.InternetBankingMobileServerFlow(loader, institutions));

                       //////new guys
                       //processors.Add(new Fingrid.Monitoring.NibssOutward(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.EODMonitor(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.EodMonitor2(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.IBBIBWebClientFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.IBBIBWebServerFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.IBVanillaWebClientFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.IBVanillaWebServerFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.InternetBankingBIBMobileClientTracker(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.ISWGatewayTransfers(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.InternetBankingBIBServerProcessFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.InternetBankingVanillaMobileClientTracker(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.InternetBankingVanillaServerProcessFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BankOneMobileBranchlessBankingClientFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.BankOneMobileBranchlessBankingInAppFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.ProvidusWebClientFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.ProvidusWebServerFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.ProvidusMobileClientFlow(loader, institutions));
                       //processors.Add(new Fingrid.Monitoring.CreditClubProcessor(loader));
                       //processors.Add(new Fingrid.Monitoring.BankOneMobileProcessor(loader));

                       //processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.AccountOpeningService(loader));
                       //processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.OfflineDepositService(loader));
                       //processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.CreditAssessmentLoanDisbursementService(loader));
                       //processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.DBNFTReversalService(loader));
                       //processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.PQAService(loader));
                       //processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.SterlingInwardsTransferService(loader));

                       processors.ForEach(p => p.StartListening());



                   }
                   catch (Exception ex)
                   {
                       Trace.TraceInformation($"Initialization Failed {ex.Message} \n Stack: {ex.StackTrace} \n Inner: {(ex.InnerException != null ? ex.InnerException.Message : "NULL")}");

                       throw;
                   }
                   finally
                   {
                       isBusy = false;
                   }
               }
                ));

            thWorker.Start();
            //this.timer = new System.Timers.Timer();
            //this.timer.Elapsed += new ElapsedEventHandler(this.timer_Elapsed);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {0}", DateTimeOffset.Now);
                //Logger.Log(string.Format("Service started"));
                lastMfbChannelsReportRunTime = DateTime.Now.AddHours(-2); //ensures that the report is run immediately
                StartUpEngine();
                await Task.Delay(SyncInterval(), stoppingToken);
            }
        }

        private static int SyncInterval()
        {
            int interval =((24 * 60) - ((DateTime.Now.Hour * 60) + DateTime.Now.Minute)) * 60000;
            //Trace.TraceInformation(string.Format($"Next execution time in milliseconds is {interval}"));
            return interval;
        }

        public List<Institution> LoadInstitutions()
        {
            Institution institution = Fingrid.Monitoring.Utility.InstitutionInfo.GetInstitutionByCode("000000");
            List<Institution> institutions = Fingrid.Monitoring.Utility.InstitutionInfo.GetInstitutions();
            Trace.TraceInformation(institution.Name);
            Trace.TraceInformation(institutions.Count.ToString());
            foreach (var i in institutions.OrderBy(i => i.Code))
            {
                Trace.TraceInformation(i.Code + " : " + i.Name);
            }

            System.Threading.Thread.Sleep(1000);
            return institutions;
        }

        //public void DoWork()
        //{
        //    try
        //    {

        //        Fingrid.Monitoring.Utility.Institution institution = Fingrid.Monitoring.Utility.InstitutionInfo.GetInstitutionByCode("000000");
        //        List<Fingrid.Monitoring.Utility.Institution> institutions = Fingrid.Monitoring.Utility.InstitutionInfo.GetInstitutions();
        //        Trace.TraceInformation(institution.Name);
        //        Trace.TraceInformation(string.Format($"institution count is {institutions.Count}"));
        //        foreach (var i in institutions.OrderBy(i => i.Code))
        //        {
        //            Trace.TraceInformation(string.Format($"{i.Code} : {i.Name}"));
        //        }

        //        System.Threading.Thread.Sleep(1000);
        //        System.Threading.ThreadPool.SetMinThreads(4, 10);
        //        Fingrid.Monitoring.Loader loader = new Fingrid.Monitoring.Loader();
        //        loader.LoadAll();

        //        //Give time to get everything connected.
        //        System.Threading.Thread.Sleep(5000);

        //        //List<Fingrid.Monitoring.BaseProcessor> processors = new List<Fingrid.Monitoring.BaseProcessor>() {
        //        //     new Fingrid.Monitoring.CbaProcessor(loader)
        //        //};
        //        Trace.TraceInformation(("check USSD"));

        //        List<Fingrid.Monitoring.BaseProcessor> processors = new List<Fingrid.Monitoring.BaseProcessor>() {

        //        //new Fingrid.Monitoring.GoGridCentralSwitchProcessor(loader, institutions), // works
        //        //new Fingrid.Monitoring.CoreBankingSwitchProcessor(loader, institutions), // works
        //        //new Fingrid.Monitoring.UssdProcessor(loader, institutions), // works
        //        //new Fingrid.Monitoring.CbaProcessor(loader, institutions), // works
        //        //new Fingrid.Monitoring.EODCoordinatorProcessor(loader), // works
        //        //new Fingrid.Monitoring.CreditClubUptime(loader), // works
        //        //new Fingrid.Monitoring.BankOneMobileUptime(loader), // works
        //        //new Fingrid.Monitoring.BankoneMessagingProcessor(loader), // works
        //        //new Fingrid.Monitoring.ExternalThirdParty(loader, institutions), // works
        //        //new Fingrid.Monitoring.BankOneAgent(loader, institutions), // works
        //        //new Fingrid.Monitoring.BankOneCacheDuration(loader, institutions), // works
        //        //new Fingrid.Monitoring.MyBankOneRequestDuration(loader), // works
        //        //new Fingrid.Monitoring.NibbsInward(loader, institutions), // works
        //        //new Fingrid.Monitoring.BankOneWebAPIMonitor(loader, institutions), // works
        //        //new Fingrid.Monitoring.CreditClubMobileTracker(loader), //works
        //        //new Fingrid.Monitoring.BankOneMobileTracker(loader), //works

        //        //new Fingrid.Monitoring.iPNXSwitchProcessor(loader, institutions), // not in use

        //        //new Fingrid.Monitoring.CbaIsoProcessor(loader, institutions), // Crashing with index out of bound
        //        //new Fingrid.Monitoring.CbaIsoProcessor2(loader, institutions),  // Crashing with index out of bound

        //        //new Fingrid.Monitoring.BankOneAnniversaryNotificationProcessor(loader), // didnt get any entry for a while but from observation of previous classes i think it works | test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.BatchAccountStatementGeneration(loader), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.PreConfirmationProcessor(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.USSDSimulatorMonitoring(loader), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.InternetBankingProcessor(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.InternalThirdParty(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.CorrespondentBk_App(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.CorrespondentBk_Web(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use 
        //        //new Fingrid.Monitoring.BankOneBranchlessBankingClientFlow(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.BranchlessBankingServerFlow(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.BankOneMobileClientFlow(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.BankOneMobileServerFlow(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.InternetBankingMobileClientFlow(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use
        //        //new Fingrid.Monitoring.InternetBankingMobileServerFlow(loader, institutions), // didnt get any entry for a while but from observation of previous classes i think it works || test in the weekdays | might not be in use

        //        //new guys
        //        new Fingrid.Monitoring.NibssOutward(loader, institutions), // works
        //        //new Fingrid.Monitoring.EODMonitor(loader, institutions), // works
        //        //new Fingrid.Monitoring.EodMonitor2(loader, institutions), // works
        //        //new Fingrid.Monitoring.IBBIBWebClientFlow(loader, institutions), //works
        //        //new Fingrid.Monitoring.IBBIBWebServerFlow(loader, institutions), //works
        //        //new Fingrid.Monitoring.IBVanillaWebClientFlow(loader, institutions), //works
        //        //new Fingrid.Monitoring.IBVanillaWebServerFlow(loader, institutions),//works
        //        //new Fingrid.Monitoring.InternetBankingBIBMobileClientTracker(loader, institutions), //works
        //        //new Fingrid.Monitoring.ISWGatewayTransfers(loader, institutions),//works
        //        //new Fingrid.Monitoring.InternetBankingBIBServerProcessFlow(loader, institutions), //works
        //        //new Fingrid.Monitoring.InternetBankingVanillaMobileClientTracker(loader, institutions), // works
        //        //new Fingrid.Monitoring.InternetBankingVanillaServerProcessFlow(loader, institutions), // works
        //        //new Fingrid.Monitoring.BankOneMobileBranchlessBankingClientFlow(loader, institutions),
        //        //new Fingrid.Monitoring.BankOneMobileBranchlessBankingInAppFlow(loader, institutions),
        //        //new Fingrid.Monitoring.ProvidusWebClientFlow(loader, institutions),
        //        //new Fingrid.Monitoring.ProvidusWebServerFlow(loader, institutions),
        //        //new Fingrid.Monitoring.ProvidusMobileClientFlow(loader, institutions),
        //        };
                
        //        System.Console.WriteLine("Done USSD");
        //        System.Console.WriteLine($"Processor Count:{processors.Count}");
        //        Trace.TraceInformation("Done USSD");
        //        Trace.TraceInformation($"Processor Count:{processors.Count}");
        //        //processors.ForEach(p => p.StartListening());
        //        var count = 0;
        //        foreach (var processor in processors)
        //        {
        //            count++;
        //            System.Console.WriteLine($"Processor Count: {count}");
        //            Trace.TraceInformation($"Processor Count: {count}");

        //            processor.StartListening();
        //        }

        //        //mfb Channels Report Generator
        //        if (DateTime.Now.Subtract(lastMfbChannelsReportRunTime).TotalHours > 1)
        //        {
        //            Trace.TraceInformation("About to run MFB channels Report");
        //            lastMfbChannelsReportRunTime = DateTime.Now;
        //            new Fingrid.Monitoring.MFBChannelsReportGenerator(loader, "SwitchTransactions");
        //            Trace.TraceInformation("Done with running MFB channels Report");
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceInformation(string.Format($"Error occured here {ex.Message} | {ex.InnerException?.Message} - {ex.StackTrace}"));
        //    }
        //    _timer.Change(Processor.SyncInterval(), Timeout.Infinite);
        //}



        //private void timer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    if (!isBusy)
        //    {
        //        this.timer.Stop();


        //        try
        //        {
        //            var section = System.Configuration.ConfigurationManager.GetSection("ApplicationUptimeUrls") as Hashtable;
        //            Trace.TraceInformation("converted to hash successfully");
        //            var urls = section.Cast<DictionaryEntry>().ToDictionary(d => d.Key.ToString(), d => d.Value.ToString());
        //            Trace.TraceInformation("converted to dictionary successfully");


        //            Parallel.ForEach(urls, item => Process(item.Key, item.Value));

        //            this.timer.Interval = 1000 * Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["TimerInterval"]);

        //        }
        //        catch (Exception ex)
        //        {
        //            Trace.TraceInformation($"An Error Occured {ex.Message} \n Stack: {ex.StackTrace} \n Inner: {(ex.InnerException != null ? ex.InnerException.Message : "NULL")}");
        //        }

        //        finally
        //        {
        //            this.timer.Start();
        //        }
        //    }
        //}
        public void Start()
        {
            this.timer.Start();
        }
        public void Stop()
        {
            if (timer != null)
            {
                this.timer.Stop();
            }
        }
        //private void Process(string appName, string url)
        //{
        //    string uniqueId = Guid.NewGuid().ToString();
        //    string publishInfo = $"{uniqueId},{DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss:fffffff tt ")}, {appName},false";
        //    string response = string.Empty;
        //    PublishRequest(appName, publishInfo);

        //    using (var client = new CreditClubAppMon.AppMonitoringServiceClient())
        //    {
        //        client.Endpoint.Address = new System.ServiceModel.EndpointAddress(url);
        //        try
        //        {
        //            response = client.Run();
        //        }
        //        catch (Exception ex)
        //        {
        //            Trace.TraceInformation($"{appName} Error: {ex.Message} \n Stack: {ex.StackTrace}");
        //            response = "14";
        //        }
        //        finally
        //        {
        //            publishInfo = $"{uniqueId},{DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss:fffffff tt")}, {appName},true,{response}";
        //            PublishRequest(appName, publishInfo);
        //        }


        //    }

        //}
        private void PublishRequest(string _channelName, string value)
        {
            try
            {
                if (shouldPublishToRedis & !string.IsNullOrEmpty(publisherUrl))
                {
                    Trace.TraceInformation($"About to publish request {value}");
                    var url = $"{publisherUrl}?channelName={_channelName}&Message={value}";

                    string result = string.Empty;
                    var http = new HttpClient();
                    StringContent content = new StringContent("", Encoding.UTF8, "application/json");
                    using (var resp = http.PostAsync(url, content).Result)
                    {
                        if (resp.IsSuccessStatusCode)
                            result = resp.Content.ReadAsStringAsync().Result;
                        else
                            Trace.TraceInformation($"Failed to  publish request {value}, Message {resp.Content.ReadAsStringAsync()?.Result}");

                    }
                }

            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"An error occured on publish request {value}, Message {ex.Message}, stacktrace {ex.StackTrace} ,\n InnerExceptions {GetException(ex)}");

            }
        }

        private string GetException(Exception ex)
        {
            Exception exx = ex.InnerException;
            string mes = string.Empty;
            while (exx != null)
            {

                mes += exx.Message;
                exx = exx.InnerException;
            }
            return mes;

        }
    }
}
