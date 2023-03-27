using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSync
{
    internal abstract class Message
    {
        public PackageType Type = PackageType.Text;

        public DateTime SendTime { get; set; }

        public Message(PackageType type)
        {
            Type = type;
            SendTime = DateTime.Now;
        }
    }
}
