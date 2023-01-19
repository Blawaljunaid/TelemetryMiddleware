using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.ABSServiceProcessor
{
    public class DBNFTReversalService : ABSServiceMonitor
    {
        public DBNFTReversalService(Loader loader) : 
            base(loader, "__Monitoring.DBNFTReversalService.Test", "BankOneMobile")
        {
        }
    }
}
