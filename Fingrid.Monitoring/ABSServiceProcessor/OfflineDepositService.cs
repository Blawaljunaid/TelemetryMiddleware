using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.ABSServiceProcessor
{
   public  class OfflineDepositService: ABSServiceMonitor
    {
        public OfflineDepositService(Loader loader):
            base(loader, "__Monitoring.OfflineDepositService.Test", "BankOneMobile")
        {

        }
    }
}
