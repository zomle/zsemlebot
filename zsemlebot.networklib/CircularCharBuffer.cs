using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zsemlebot.networklib
{
    public class CircularCharBuffer : CircularBuffer<char>
    {
        public CircularCharBuffer(int capacity)
            : base(capacity)
        {
        }

        public void PushData(string data)
        {
            PushData(data.ToCharArray());
        }

        public bool TryReadLine(out string result)
        {
            const string nl = "\r\n";

            var newLineIndex = FirstIndexOf(nl.ToCharArray());
            if (newLineIndex == -1)
            {
                result = null;
                return false;
            }

            if (!TryRead(newLineIndex, out var tmp))
            {
                result = null;
                return false;
            }

            Skip(nl.Length);

            result = new string(tmp);
            return true;
        }
    }
}
