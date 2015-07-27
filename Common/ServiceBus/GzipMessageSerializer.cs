using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ServiceBus
{
    public class GzipMessageSerializer : IMessageSerializer
    {
        public string ContentEncoding
        {
            get
            {
                return "gzip";
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
            string content = null;
            using (var ms = new MemoryStream(value))
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(gzip))
                    {
                        content = reader.ReadToEnd();
                    }
                }
            }

            if (string.IsNullOrEmpty(content))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(content);
        }

        public byte[] SerializeObject<T>(T value)
        {
            string content = JsonConvert.SerializeObject(value);

            byte[] gzipValue = null;

            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    var writer = new StreamWriter(gzip);

                    writer.Write(content);
                    writer.Flush();

                    gzip.Flush();
                    gzip.Close();

                    gzipValue = ms.ToArray();

                }
            }

            return gzipValue;
        }
    }
}
