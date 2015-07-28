using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.ServiceBus
{
    /// <summary>
    /// When raised the consumer process will exit if IsRetryable is false and StopSignal is true
    /// if IsRetryable is false and StopSignal is false the message will be ignored and deleted
    /// </summary>
    [Serializable]
    public class ConsumerException : Exception
    {
        public bool IsRetryable { get; set; }
        public bool StopSignal { get; set; }

        public ConsumerException()
            : base()
        { }

        public ConsumerException(string message)
            : base(message)
        { }

        public ConsumerException(string format, params object[] args)
            : base(string.Format(format, args))
        { }

        public ConsumerException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ConsumerException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException)
        { }
    }
}
