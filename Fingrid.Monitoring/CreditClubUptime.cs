using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class CreditClubUptime : BaseMobileUptime
    {
        public CreditClubUptime(Loader loader) : base(loader, "CreditClub_Uptime_Live")
        {
        }

       
    }
}
