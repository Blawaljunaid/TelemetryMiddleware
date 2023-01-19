using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class BankOneMobileUptime : BaseMobileUptime
    {
        public BankOneMobileUptime(Loader loader) : base(loader, "BankOneMobile_Uptime_Live")
        {
        }


    }
}
