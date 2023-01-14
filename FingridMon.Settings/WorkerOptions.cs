namespace FingridMon.Settings
{
    public class WorkerOptions
    {
        public string? ABServiceMonitoringSuccessStatus { get; set; }
        public string? BANKONEAPI { get; set; }
        public string? BANKONEWEBAPI { get; set; }
        public string? RedisConnection { get; set; }
        public string? InfluxDBUrl { get; set; }
        public string? InfluxDBUserName { get; set; }
        public string? InfluxDBPassword { get; set; }
        public string? TimerInterval { get; set; }
        public string? MobileTechnicalFailureCodes { get; set; }
        public string? TestInstitutionCodes { get; set; }
        public string? IntegrationFIs { get; set; }
        public string? InstitutionSpecificTechnicalSuccessCodes { get; set; }
        public string? PublisherUrl { get; set; }
        public string? shouldPublishToRedis { get; set; }
        public string? timeTakenThreshold { get; set; }
        public string? FepNodeName { get; set; }
        public string? TechnicalSuccessCodes { get; set; }
        public string? USSDTechnicalSuccessCodes { get; set; }
        public string? USSDTestInstitutionCodes { get; set; }
        public string? requestTimeTakenThreshold { get; set; }
        public string? responseTimeTakenThreshold { get; set; }
        public string? shouldSubscribeToRedis { get; set; }
        public string? shouldSubscribeToEventStore { get; set; }
        public string? InternetBankingTestInstitutionCodes { get; set; }
        public string? InternetBankingSuccessCodes { get; set; }
        public string? EventStoreConnection { get; set; }
        public string? PostingTypeFileName { get; set; }
        public string? ChannelAccessCodesFileName { get; set; }
        public string? ServiceCodesAndChannelAccessCodesFileName { get; set; }
        public string? BanKInTheBoxFIs { get; set; }
    }
}