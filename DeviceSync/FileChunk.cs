using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSync
{
    internal class FileChunk : Message
    {
        public byte[] Content { get; set; }
        public string FileName { get; set; }

        public FileChunk(PackageType type, byte[] content, string fileName) : base(type)
        {
            Content = content;
            FileName = fileName;
        }
    }
}
