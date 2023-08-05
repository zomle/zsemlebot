using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zsemlebot.networklib
{
    public class CircularBuffer<T>
    {
        public int Capacity { get; private set; }

        public int DataLength
        {
            get
            {
                if (EndIndex < StartIndex)
                {
                    return Capacity - StartIndex + EndIndex;
                }
                else
                {
                    return EndIndex - StartIndex;
                }
            }
        }

        public T this[int index]
        {
            get
            {
                if (DataLength <= index)
                {
                    throw new IndexOutOfRangeException("index");
                }

                return Buffer[(StartIndex + index) % Capacity];
            }
        }

        private T[] Buffer { get; set; }
        private int StartIndex { get; set; }
        private int EndIndex { get; set; }

        private static readonly object padlock = new object();

        public CircularBuffer(int capacity)
        {
            Capacity = capacity > 10 ? capacity : 10;
            Buffer = new T[capacity];
        }

        public int FirstIndexOf(T[] input)
        {
            for (int i = 0; i <= DataLength - input.Length; ++i)
            {
                bool found = true;
                for (int j = 0; j < input.Length; ++j)
                {
                    if (!EqualityComparer<T>.Default.Equals(this[i + j], input[j]))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool TryPeek(int length, out T[] result)
        {
            lock (padlock)
            {
                if (length == 0 || length > DataLength)
                {
                    result = Array.Empty<T>();
                    return false;
                }

                result = new T[length];

                int dataUntilWrap = Capacity - StartIndex;
                if (dataUntilWrap < length)
                {
                    Array.Copy(Buffer, StartIndex, result, 0, dataUntilWrap);
                    Array.Copy(Buffer, 0, result, dataUntilWrap, length - dataUntilWrap);
                }
                else
                {
                    Array.Copy(Buffer, StartIndex, result, 0, length);
                }
                return true;
            }
        }

        public bool TryRead(int length, out T[] result)
        {
            lock (padlock)
            {
                if (TryPeek(length, out result))
                {
                    StartIndex = (StartIndex + length) % Capacity;
                    return true;
                }

                return false;
            }
        }

        public void Skip(int length)
        {
            lock (padlock)
            {
                if (length > DataLength)
                {
                    length = DataLength;
                }

                StartIndex = (StartIndex + length) % Capacity;
            }
        }

        public void PushData(T[] data)
        {
            lock (padlock)
            {
                PushData(data, 0, data.Length);
            }
        }

        public void PushData(T[] data, int startIndex, int length)
        {
            lock (padlock)
            {
                if (data.Length < startIndex + length)
                {
                    throw new InvalidOperationException("Buffer length is smaller than expected.");
                }

                //expand buffer
                if (length + DataLength > Capacity)
                {
                    var newCapacity = length + DataLength + 1000;
                    var newBuffer = new T[newCapacity];
                    if (EndIndex < StartIndex)
                    {
                        Array.Copy(Buffer, StartIndex, newBuffer, 0, Capacity - StartIndex);
                        Array.Copy(Buffer, 0, newBuffer, Capacity - StartIndex, EndIndex);
                    }
                    else
                    {
                        Array.Copy(Buffer, StartIndex, newBuffer, 0, EndIndex - StartIndex);
                    }

                    var dataLength = DataLength;
                    StartIndex = 0;
                    Capacity = newCapacity;
                    EndIndex = dataLength;

                    Buffer = newBuffer;
                }

                //wrapped
                if (EndIndex < StartIndex)
                {
                    Array.Copy(data, startIndex, Buffer, EndIndex, length);
                    EndIndex += length;
                }
                else
                {
                    int spaceUntilWrap = Capacity - EndIndex;
                    if (spaceUntilWrap >= length)
                    {
                        Array.Copy(data, startIndex, Buffer, EndIndex, length);
                        EndIndex += length;
                    }
                    else
                    {
                        Array.Copy(data, startIndex, Buffer, EndIndex, spaceUntilWrap);
                        Array.Copy(data, startIndex + spaceUntilWrap, Buffer, 0, length - spaceUntilWrap);
                        EndIndex = length - spaceUntilWrap;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"StartIx: {StartIndex}; EndIx: {EndIndex}; Length: {DataLength}; Capacity: '{Capacity}'";
        }

        private T[] ReadAll()
        {
            var result = new T[DataLength];

            if (EndIndex < StartIndex)
            {
                Array.Copy(Buffer, StartIndex, result, 0, Capacity - StartIndex);
                Array.Copy(Buffer, 0, result, Capacity - StartIndex, EndIndex);
            }
            else
            {
                Array.Copy(Buffer, StartIndex, result, 0, EndIndex - StartIndex);
            }
            return result;
        }

    }
}
