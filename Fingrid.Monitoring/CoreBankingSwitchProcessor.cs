using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
   public  class CoreBankingSwitchProcessor: SwitchProcessor
    {
        public CoreBankingSwitchProcessor(Loader loader, List<Institution> institutions):
            base(loader, "__Monitoring.BankOne.Switch.CoreBanking", "Azure CBA Switch", institutions)
        {

        }
    }
}
