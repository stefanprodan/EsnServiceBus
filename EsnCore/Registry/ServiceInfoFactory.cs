using EsnCore.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.Registry
{
    public class ServiceInfoFactory
    {
        public static ServiceInfo CreateServiceDefinition(ServiceInfo ms = null)
        {
            if (ms == null)
            {
                ms = new ServiceInfo();
                ms.State = ServiceState.Running;
                ms.Tags = new List<string>();
            }

            ms.Host = new HostInfo
            {
                Guid = GetUUID(),
                Name = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                CPUs = Environment.ProcessorCount,
                TotalMemory = GetTotalMemory(),
                FreeMemory = GetFreeMemory(),
            };

            ms.HostGuid = ms.Host.Guid;
            ms.BuildDate = ProcessHelpers.GetBuildTimestamp();
            ms.Version = ProcessHelpers.GetBuildVersion();
            ms.HostName = Environment.MachineName;
            ms.LocalPath = Environment.CurrentDirectory;
            ms.MemoryUsage = Convert.ToInt64((Environment.WorkingSet / 1024f) / 1024f);
#if DEBUG
            if (!ms.Tags.Contains("debug"))
            {
                ms.Tags.Add("debug");
            }
#endif
            using (var proc = Process.GetCurrentProcess())
            {
                ms.Pid = proc.Id;
                ms.Name = proc.ProcessName.Replace(".exe", string.Empty);
                ms.StartDate = proc.StartTime.ToUniversalTime();
                ms.CpuTime = proc.TotalProcessorTime;
            }

            if (ms.Port == 0)
            {
                ms.Port = ProcessHelpers.GetFreeTcpPort();
            }

            return ms;
        }

        private static long GetTotalMemory()
        {
            var computerSystem = GetFirstObject("Select * from Win32_ComputerSystem");
            return Convert.ToInt64(computerSystem.Properties["TotalPhysicalMemory"].Value);
        }

        private static string GetUUID()
        {
            // http://azure.microsoft.com/blog/2014/10/13/accessing-and-using-azure-vm-unique-id/
            var computerSystem = GetFirstObject("Select * from Win32_ComputerSystemProduct");
            return computerSystem["UUID"].ToString();

        }

        private static long GetFreeMemory()
        {
            var pc = new PerformanceCounter("memory", "Available Bytes");
            return Convert.ToInt64(pc.NextValue());
        }

        private static ManagementBaseObject GetFirstObject(string query)
        {
            var results = new ManagementObjectSearcher(query)
                           .Get()
                           .OfType<ManagementBaseObject>();
            var firstObject = results.First();

            return firstObject;
        }
    }
}
