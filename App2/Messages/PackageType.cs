using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSync.Messages
{
    internal enum PackageType
    {
        Text,
        FileChunk,
        Acknowledge,
        KeyEvent,
        Command
    }
}
