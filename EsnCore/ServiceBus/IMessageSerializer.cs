using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.ServiceBus
{
    public interface IMessageSerializer
    {
        byte[] SerializeObject<T>(T value);
        T DeserializeObject<T>(byte[] value);
        string ContentType { get; }
        string ContentEncoding { get; }
    }
}
