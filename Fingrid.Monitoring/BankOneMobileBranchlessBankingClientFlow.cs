using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    

    public class BankOneMobileBranchlessBankingClientFlow : InternetBankingClientMobileTracker
    {
        public BankOneMobileBranchlessBankingClientFlow(Loader loader, List<Institution> institutions) :
            base(loader, institutions, "BranchlessBankingClient", "BankOneMobile.BranchlessBanking.Flow")
        {

        }
    }
}
