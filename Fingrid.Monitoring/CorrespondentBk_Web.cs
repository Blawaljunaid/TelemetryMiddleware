using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class CorrespondentBk_Web: CorrespondentBanking
    {
        public CorrespondentBk_Web(Loader loader, List<Institution> institutions) :
            base(loader, "CorrespondentBanking_Web", "CorrespondentBanking_Web", institutions)
        {

        }
    }
}
