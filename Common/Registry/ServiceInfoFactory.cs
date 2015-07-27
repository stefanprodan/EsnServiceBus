using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Registry
{
    public class ServiceInfoFactory
    {
        public static ServiceInfo CreateServiceDefinition(ServiceInfo ms = null)
        {
            if (ms == null)
            {
                ms = new ServiceInfo();
                ms.State = ServiceState.NotRegistered;
            }

            ms.BuildDate = ProcessHelpers.GetBuildTimestamp();
            ms.Version = ProcessHelpers.GetBuildVersion();
            ms.HostName = Environment.MachineName;
            ms.OSVersion = Environment.OSVersion.ToString();
            ms.LocalPath = Environment.CurrentDirectory;
            ms.MemoryUsage = Convert.ToInt64((Environment.WorkingSet / 1024f) / 1024f);

            using (var proc = Process.GetCurrentProcess())
            {
                ms.Pid = proc.Id;
                ms.Name = proc.ProcessName.Replace(".exe", string.Empty);
                ms.StartDate = proc.StartTime.ToUniversalTime();
            }

            if (ms.Port == 0)
            {
                ms.Port = ProcessHelpers.GetFreeTcpPort();
            }

            return ms;
        }
    }
}
