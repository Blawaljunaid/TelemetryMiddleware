using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class InternetBankingBIBServerProcessFlow : BaseMobileServerFlow
    {
        public InternetBankingBIBServerProcessFlow(Loader loader, List<Institution> institutions) :
            base(loader, institutions, "InternetBankingBIBServerProcessFlow", "InternetBanking.BiB.MobileTransaction.ServerFlow")
        {

        }
    }
}
