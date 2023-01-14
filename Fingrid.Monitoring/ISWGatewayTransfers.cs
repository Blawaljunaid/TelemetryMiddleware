
using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class ISWGatewayTransfers : Thirdparty
    {
        public ISWGatewayTransfers(Loader loader, List<Institution> institutions) :
            base(loader, "ISWGateway_Live", "ISWGatewayTransfers", institutions)
        {

        }
    }
}
