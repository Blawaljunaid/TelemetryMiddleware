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
    public class IBBIBWebClientFlow: BaseIBWebClientFlow
    {

            public IBBIBWebClientFlow(Loader loader, List<Institution> institutions) :
            base(loader, "InternetBanking.Web.BankInABox.ClientFlow", "IBBIBWebClientFlow", institutions)
        {

        }

    }
}

