using Fingrid.Monitoring.Utility;
using InfluxDB.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{

    public class BankOneMobileTracker : BaseMobileTracker
    {
        public BankOneMobileTracker(Loader loader) : base(loader, "BankOneMobileTracker", "BankOneMobile.MobileTracker")
        {
            Logger.Log("BankOneMobileTracker");
        }

    }
}
