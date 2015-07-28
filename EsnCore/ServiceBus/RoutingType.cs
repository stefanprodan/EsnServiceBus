using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.ServiceBus
{
    public enum RoutingType
    {
        Topic = 0,
        Fanout = 1,
        Direct = 2,
    }
}
