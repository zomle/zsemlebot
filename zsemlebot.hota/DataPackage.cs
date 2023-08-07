using System;
using zsemlebot.hota.Extensions;

namespace zsemlebot.hota
{
    public class DataPackage
    {
        public readonly short Type;
        public byte[] Content { get { return content; } }
        public bool SkipLogging { get; set; }

        private readonly byte[] content;

        public DataPackage(byte[] rawContent)
        {
            var length = rawContent.ReadShort(0);
            if (length != rawContent.Length)
            {
                throw new InvalidOperationException("Package reported length != actual byte[] length");
            }

            Type = rawContent.ReadShort(2);

            content = rawContent;
        }

        public byte ReadByte(int index)
        {
            return content[index];
        }

        public string ReadString(int index, int len)
        {
            return content.ReadString(index, len);
        }

        public byte[] ReadBytes(int index, int len)
        {
            return content.ReadBytes(index, len);
        }

        public short ReadShort(int index)
        {
            return content.ReadShort(index);
        }

        public int ReadInt(int index)
        {
            return content.ReadInt(index);
        }
    }
}
