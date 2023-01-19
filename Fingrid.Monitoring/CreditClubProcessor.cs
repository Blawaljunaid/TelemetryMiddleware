using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
   public  class CreditClubProcessor: MobileProcessor
    {
        public CreditClubProcessor(Loader loader):
            base(loader, "__Monitoring.CreditClubTrx.Live", "CreditClub")
        {

        }
    }
}
