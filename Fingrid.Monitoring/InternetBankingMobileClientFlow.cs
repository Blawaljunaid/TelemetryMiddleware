using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class InternetBankingMobileClientFlow: InternetBankingClientMobileTracker
    {
        public InternetBankingMobileClientFlow(Loader loader, List<Institution> institutions) :
            base(loader, institutions, "InternetBankingMobileClientFlow", "InternetBanking.MobileTransaction.ClientFlow")
        {

        }
    }
}
