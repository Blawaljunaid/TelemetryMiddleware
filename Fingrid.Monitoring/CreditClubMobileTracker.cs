using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{

    public class CreditClubMobileTracker : BaseMobileTracker
    {
        public CreditClubMobileTracker(Loader loader) : base(loader, "CreditClubMobileTracker", "CreditClub.MobileTracker")
        {
            Logger.Log("CreditClubMobileTracker");
        }

    }
}
