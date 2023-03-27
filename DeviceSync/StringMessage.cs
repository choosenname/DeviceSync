﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSync
{
    internal class StringMessage : Message
    {
        public string Content { get; set; }

        public StringMessage(PackageType type, string content) : base(type)
        {
            Content = content;
        }
    }
}