using Fingrid.Monitoring.Utility;
using InfluxDB.Net;
using InfluxDB.Net.Infrastructure.Influx;
using InfluxDB.Net.Models;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class IBVanillaWebServerFlow: BaseIBWebServerFlow
    {
       
            public IBVanillaWebServerFlow(Loader loader, List<Institution> institutions) :
            base(loader, "InternetBanking.Web.Vanilla.ServerFlow", "IBVanillaWebServerFlow", institutions)
        {

        }

    }
}
