using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ServiceBus
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public string ContentEncoding
        {
            get
            {
                return "utf-8";
            }
        }

        public string ContentType
        {
            get
            {
                return "application/json";
            }
        }

        public T DeserializeObject<T>(byte[] value)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(value));
        }

        public byte[] SerializeObject<T>(T value)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        }
    }
}
