using System;
using System.Collections.Generic;

namespace EsnCore.ServiceBus
{
    public class ConsumerExitEventArgs : EventArgs
    {
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public string RoutingKey { get; set; }
        public bool StopSignalReceived { get; set; }
        public byte[] Message { get; set; }
        public IDictionary<string, object> Headers { get; set; }
        public Exception UnderlyingException { get; set; }
    }
}
