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
                       processors.Add(new Fingrid.Monitoring.CoreBankingSwitchProcessor(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.UssdProcessor(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.CbaProcessor(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.EODCoordinatorProcessor(loader));
                       processors.Add(new Fingrid.Monitoring.CreditClubUptime(loader));
                       processors.Add(new Fingrid.Monitoring.BankOneMobileUptime(loader));
                       processors.Add(new Fingrid.Monitoring.BankoneMessagingProcessor(loader));
                       processors.Add(new Fingrid.Monitoring.ExternalThirdParty(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankOneAgent(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankOneCacheDuration(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.MyBankOneRequestDuration(loader));
                       processors.Add(new Fingrid.Monitoring.NibbsInward(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankOneWebAPIMonitor(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.CreditClubMobileTracker(loader));
                       processors.Add(new Fingrid.Monitoring.BankOneMobileTracker(loader));

                       processors.Add(new Fingrid.Monitoring.iPNXSwitchProcessor(loader, institutions)); // not in use

                       // processors.Add(new Fingrid.Monitoring.CbaIsoProcessor(loader, institutions)); // Crashing with index out of bound
                       // processors.Add(new Fingrid.Monitoring.CbaIsoProcessor2(loader, institutions));  // Crashing with index out of bound

                       processors.Add(new Fingrid.Monitoring.BankOneAnniversaryNotificationProcessor(loader));
                       processors.Add(new Fingrid.Monitoring.BatchAccountStatementGeneration(loader));
                       processors.Add(new Fingrid.Monitoring.PreConfirmationProcessor(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.USSDSimulatorMonitoring(loader));
                       processors.Add(new Fingrid.Monitoring.InternetBankingProcessor(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.InternalThirdParty(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.CorrespondentBk_App(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.CorrespondentBk_Web(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankOneBranchlessBankingClientFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BranchlessBankingServerFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankOneMobileClientFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankOneMobileServerFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.InternetBankingMobileClientFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.InternetBankingMobileServerFlow(loader, institutions));

                       ////new guys
                       processors.Add(new Fingrid.Monitoring.NibssOutward(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.EODMonitor(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.EodMonitor2(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.IBBIBWebClientFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.IBBIBWebServerFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.IBVanillaWebClientFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.IBVanillaWebServerFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.InternetBankingBIBMobileClientTracker(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.ISWGatewayTransfers(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.InternetBankingBIBServerProcessFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.InternetBankingVanillaMobileClientTracker(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.InternetBankingVanillaServerProcessFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankOneMobileBranchlessBankingClientFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankOneMobileBranchlessBankingInAppFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.ProvidusWebClientFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.ProvidusWebServerFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.ProvidusMobileClientFlow(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.CreditClubProcessor(loader));
                       processors.Add(new Fingrid.Monitoring.BankOneMobileProcessor(loader));

                       processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.AccountOpeningService(loader));
                       processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.OfflineDepositService(loader));
                       processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.CreditAssessmentLoanDisbursementService(loader));
                       processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.DBNFTReversalService(loader));
                       processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.PQAService(loader));
                       processors.Add(new Fingrid.Monitoring.ABSServiceProcessor.SterlingInwardsTransferService(loader));

                       //Latest guys
                       processors.Add(new Fingrid.Monitoring.QoreBIBTransactionProcessor(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.BankoneWebApi(loader, institutions));
                       processors.Add(new Fingrid.Monitoring.RecovaProcessor(loader));
                       processors.Add(new Fingrid.Monitoring.RecovaAuthProcessor(loader));

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
