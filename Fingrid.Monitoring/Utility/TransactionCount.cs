using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.Utility
{
    public class TransactionCount
    {
        public string InstitutionName { get; set; }
        public string ResponseCode { get; set; }
        public string TechnicalSuccess { get; set; }
        public long FailureCount { get; set; }
        public long ResponseCount { get; set; }
        public long TotalCount { get; set; }
        public long ResponseSuccessCount { get; set; }
        public long TotalSuccessCount { get; set; }
        public double SuccessPercentage { get; set; }
        public string SwitchTransactionType { get; set; }
    }
}
