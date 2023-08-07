using System;

namespace zsemlebot.networklib
{
    public class CircularByteBuffer : CircularBuffer<byte>
    {
        public CircularByteBuffer(int capacity)
            : base(capacity)
        {
        }

        public bool TryPeekShort(out short value)
        {
            if (DataLength < 2)
            {
                value = default;
                return false;
            }

            value = (short)((this[1] << 8) + this[0]);
            return true;
        }
    }
}
