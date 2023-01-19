using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
   public  class GoGridCentralSwitchProcessor: SwitchProcessor
    {
        public GoGridCentralSwitchProcessor(Loader loader, List<Institution> institutions) :
            base(loader, "__Monitoring.BankOne.Switch.Gogrid", "Azure Central Switch", institutions)
        {

        }
    }
}
