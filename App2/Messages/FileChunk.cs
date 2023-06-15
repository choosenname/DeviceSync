using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSync.Messages
{
    internal class FileChunk : BytesMessage
    {
        public int CurrentChunk { get; set; }

        public int NumChunk { get; set; }

        public FileChunk(PackageType type, byte[] content, string fileName, int currentChunk, int numChunk) 
            : base(type, content, fileName)
        {
            CurrentChunk = currentChunk;
            NumChunk = numChunk;
        }
    }
}
