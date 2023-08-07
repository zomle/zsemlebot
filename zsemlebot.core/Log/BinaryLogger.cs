using System;
using System.IO;

namespace zsemlebot.core.Log
{
    public abstract class BinaryLogger : Logger, IDisposable
    {
        private BinaryWriter Writer { get; set; }

        public BinaryLogger(string fileName)
            : base(fileName)
        {
            Writer = new BinaryWriter(GetFileStream());
        }

        protected BinaryLogger()
            : base()
        {
            Writer = BinaryWriter.Null;
        }

        public void Write(byte[] data, int length)
        {
            Writer.Write(data, 0, length);
        }

        #region IDisposable implementation
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Writer != null)
                    {
                        Writer.Flush();
                        Writer.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
