using EsnCore.Registry;
using EsnCore.ServiceBus;
using EsnServiceRegistry.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnServiceRegistry.Consumers
{
    public class StatsConsumer : IConsumer<ServiceInfo>
    {
        public void ProcessMessage(ServiceInfo service)
        {
            try
            {
                var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
                registryRepo.InsertOrUpdateService(service);
            }
            catch (Exception ex)
            {
                var exe = new ConsumerException("Database error", ex);
                exe.IsRetryable = true;
                throw exe;
            }
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
