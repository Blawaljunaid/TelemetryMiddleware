using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
   public  class iPNXSwitchProcessor: SwitchProcessor
    {
        public iPNXSwitchProcessor(Loader loader, List<Institution> institutions) :
            base(loader, "__Monitoring.BankOne.Switch.IPNX", "IPNX Switch", institutions)
        {

        }

        //protected override string GetUniqueId(string[] inputParams)
        //{
        //    return String.Join(inputParams[1].Trim(), inputParams[7].Trim(), inputParams[8].Trim());
        //}
    }
}
