using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.Registry
{
    public enum ServiceState
    {
        NotRegistered = 0,
        StartPending = 1,
        Running = 2,
        PausePending = 3,
        Paused = 4,
        ContinuePending = 5,
        StopPending = 6,
        Stopped = 7,
    }
}
