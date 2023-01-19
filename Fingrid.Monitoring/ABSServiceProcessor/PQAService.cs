using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.ABSServiceProcessor
{
    public class PQAService : ABSServiceMonitor
    {
        public PQAService(Loader loader) : 
            base(loader, "__Monitoring.PQAService.Live", "CreditAssessment")
        {
        }
    }
}
