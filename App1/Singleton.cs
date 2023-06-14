using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSync
{
    internal class Singleton
    {
        public static Singleton Instance { get; } = new Singleton();

        public IPAddress IpAddress { get; set; }

        public int RemotePort { get; set; }

        public int LocalPort { get; set; }
    }
}
