using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class NibssOutward: Thirdparty
    {
        public NibssOutward(Loader loader, List<Institution> institutions) :
            base(loader, "__Monitoring.NibssOutward.Live", "NibssOutward", institutions)
        {

        }
    }
}
