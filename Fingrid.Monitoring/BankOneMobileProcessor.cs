using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
   public  class BankOneMobileProcessor: MobileProcessor
    {
        public BankOneMobileProcessor(Loader loader):
            base(loader, "__Monitoring.BankOneMobileTrx.Live", "BankOneMobile")
        {

        }
    }
}
