using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class InternalThirdParty:Thirdparty
    {
        public InternalThirdParty(Loader loader, List<Institution> institutions) :
            base(loader, "__Monitoring.ThirdPartyAPI_7.Live", "InternalThirdparty", institutions)
        {

        }
    }
}
