using System;
using System.Collections.Generic;
using System.Text;

namespace zsemlebot.hota.Extensions
{
    internal static class ByteArrayExtensions
    {
        public static string ReadString(this byte[] buffer, int index, int len)
        {
            int count = 0;
            for (int i = 0; i < len && buffer[i + index] != 0; ++i)
            {
                count++;
            }

            return Encoding.ASCII.GetString(buffer, index, count);
        }

        public static byte[] ReadBytes(this byte[] buffer, int index, int len)
        {
            var result = new byte[len];
            Array.Copy(buffer, index, result, 0, len);
            return result;
        }

        public static bool TryReadShort(this IReadOnlyList<byte> buffer, int index, ref short val)
        {
            if (buffer.Count < index + 2)
            {
                return false;
            }
            val = (short)((buffer[index + 1] << 8) + buffer[index]);
            return true;
        }

        public static short ReadShort(this byte[] buffer, int index)
        {
            return (short)((buffer[index + 1] << 8) + buffer[index]);
        }

        public static int ReadInt(this byte[] buffer, int index)
        {
            return (int)((buffer[index + 3] << 24) +
                         (buffer[index + 2] << 16) +
                         (buffer[index + 1] << 8) +
                          buffer[index]);
        }
        public static uint ReadUInt(this byte[] buffer, int index)
        {
           
            return (uint)((buffer[index + 3] << 24) +
                         (buffer[index + 2] << 16) +
                         (buffer[index + 1] << 8) +
                          buffer[index]);
        }

        public static void WriteByte(this byte[] buffer, int index, byte value)
        {
            buffer[index] = value;
        }

        public static void WriteShort(this byte[] buffer, int index, short value)
        {
            buffer[index] = (byte)(0xff & value);
            buffer[index + 1] = (byte)((0xff00 & value) >> 8);
        }

        public static void WriteInt(this byte[] buffer, int index, uint value)
        {
            buffer[index] = (byte)(0xff & value);
            buffer[index + 1] = (byte)((0xff00 & value) >> 8);
            buffer[index + 2] = (byte)((0xff0000 & value) >> 16);
            buffer[index + 3] = (byte)((0xff000000 & value) >> 24);
        }

        public static void WriteBytes(this byte[] buffer, int index, int maxLength, byte[] src)
        {
            for (int i = 0; i < Math.Min(src.Length, maxLength); i++)
            {
                if (i == src.Length)
                {
                    break;
                }

                buffer[index + i] = src[i];
            }
        }

        public static void WriteString(this byte[] buffer, int index, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            Array.Copy(bytes, 0, buffer, index, bytes.Length);
            buffer[index + bytes.Length] = 0;
        }
    }
}
