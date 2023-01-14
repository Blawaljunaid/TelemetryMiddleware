using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.ABSServiceProcessor
{
    public class AccountOpeningService : ABSServiceMonitor
    {
        public AccountOpeningService(Loader loader) : 
            base(loader, "__Monitoring.AccountOpeningService.Test", "BankOneMobile")
        {
        }
    }
}
