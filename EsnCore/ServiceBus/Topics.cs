using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsnCore.ServiceBus
{
    public static class Topics
    {
        public const string Text = "text";

        public const string Url = "url";

        public const string Images = "images";

        public const string Video = "video";

        public const string All = "all";

        public static string Aggregate(params string[] topics)
        {
          return  topics.Aggregate((i, j) => i + "." + j);
        }

        public static string GetBinding(string topic)
        {
            return string.Format("#.{0}.#", topic);
        }
    }
}
