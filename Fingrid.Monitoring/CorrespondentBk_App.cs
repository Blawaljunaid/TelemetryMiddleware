using Fingrid.Monitoring.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring
{
    public class CorrespondentBk_App: CorrespondentBanking
    {

        public CorrespondentBk_App(Loader loader, List<Institution> institutions) :
            base(loader, "CorrespondentBanking_App", "CorrespondentBanking_App", institutions)
        {

        }
    }
}
