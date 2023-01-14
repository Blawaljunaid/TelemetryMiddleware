using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class ExternalThirdParty: Thirdparty
    {
        public ExternalThirdParty(Loader loader, List<Institution> institutions) :
            base(loader, "__Monitoring.ThirdPartyAPI_12.Live", "ExternalThirdParty", institutions)
        {

        }
    }
}
