using System;
using System.IO;
using System.Text;

namespace zsemlebot.core.Log
{
    public abstract class TextLogger : Logger, IDisposable
    {
        private StreamWriter Writer { get; set; }

        public TextLogger(string fileName, Encoding encoding)
            : base(fileName)
        {
            Writer = new StreamWriter(GetFileStream(), encoding) { AutoFlush = true };
        }

        protected TextLogger()
            : base()
        {
            Writer = StreamWriter.Null;
        }

        public void Write(string text)
        {
            Writer.Write(text);
        }

        public void WriteLine(string text)
        {
            Writer.WriteLine(text);
        }

        public void WriteLine()
        {
            Writer.WriteLine();
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
