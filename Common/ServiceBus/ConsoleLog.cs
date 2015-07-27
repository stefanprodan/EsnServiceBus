using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ServiceBus
{
    public class ConsoleLog : ILog
    {
        public void Debug(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(string message)
        {
            Console.WriteLine(message);
        }

        public void Fatal(string message)
        {
            Console.WriteLine(message);
        }

        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void LogException(Exception exception, string message)
        {
            Console.WriteLine(message);
            Console.WriteLine(exception.StackTrace);
        }

        public void Warn(string message)
        {
            Console.WriteLine(message);
        }
    }
}
