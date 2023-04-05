using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSync.Messages
{
    internal class AcknowledgePackage : StringMessage
    {
        public int CurrentChunk { get; set; }

        public AcknowledgePackage(string path, int currentChunk) : base(PackageType.Acknowledge, path)
        {
            CurrentChunk = currentChunk;
        }
    }
}
