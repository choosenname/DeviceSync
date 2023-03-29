namespace DeviceSync.Messages
{
    internal class BytesMessage : Message
    {
        public byte[] Content { get; set; }
        public string FileName { get; set; }

        public BytesMessage(PackageType type, byte[] content, string fileName) : base(type)
        {
            Content = content;
            FileName = fileName;
        }
    }
}
