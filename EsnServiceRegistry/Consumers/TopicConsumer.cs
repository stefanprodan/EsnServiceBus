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
            var task = ProcessMessageAsync(service);
            try
            {
                task.Wait(TimeSpan.FromSeconds(30));
            }
            catch (Exception exe)
            {
                var ex = new ConsumerException("Database operation has timeout", exe);
                ex.IsRetryable = true;
                throw ex;
            }

        }

        private async Task ProcessMessageAsync(ServiceInfo service)
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                await registryRepo.InsertOrUpdateServiceAsync(service);
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
