using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.ABSServiceProcessor
{
    public class CreditAssessmentLoanDisbursementService : ABSServiceMonitor
    {
        public CreditAssessmentLoanDisbursementService(Loader loader) : 
            base(loader, "__Monitoring.CreditAssessment.LoanDisbursementService.Live", "CreditAssessment")
        {
        }
    }
}
