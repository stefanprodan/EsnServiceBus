using EsnCore.Registry;
using EsnCore.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnWorker.Indexer.Consumers
{
    public class TopicConsumer : IConsumer<ServiceInfo>
    {
        public void ProcessMessage(ServiceInfo message)
        {
            //var ex = new ConsumerException("Database server is unreachable", new TimeoutException());
            //ex.IsRetryable = false;
            //throw ex;
        }

        public void OnConsumerExit(ConsumerExitEventArgs args)
        {
            // restart service

        }

        /// <summary>
        /// Version conflict handler
        /// </summary>
        /// <param name="version">message version</param>
        public void OnVersionMismatch(Version messageVer, Version runningVer)
        {
            if (messageVer.Major > runningVer.Major)
            {
                throw new ConsumerException("Consumer needs upgrade")
                {
                    StopSignal = true,
                    IsRetryable = false,
                };
            }

            // download new version and upgrade service
        }
    }
}
