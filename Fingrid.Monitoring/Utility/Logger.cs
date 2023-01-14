using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingrid.Monitoring.Utility
{
    public class Logger
    { 
        public static void Log (string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                Console.WriteLine(message, args);
                Trace.TraceInformation(message, args);
                return;
            }
            Console.WriteLine(message);
            Trace.TraceInformation(message);
        }
    }
}
