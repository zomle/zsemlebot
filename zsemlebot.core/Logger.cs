using System;
using System.IO;

namespace zsemlebot.core
{
    public abstract class Logger : IDisposable
    {
        public string FileName { get; }
        public string FilePath { get; }
        private StreamWriter Writer { get; set; }

        public Logger(string fileName)
        {
            FileName = fileName;
            FilePath = Path.Combine(Configuration.Instance.Global.RootDirectory, Configuration.Instance.Global.LogDirectory, fileName);

            Writer = new StreamWriter(new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read));
        }

        public void Write(string text)
        {
            Writer.Write(text);
            Writer.Flush();
        }

        public void WriteLine(string text)
        {
            Writer.WriteLine(text);
            Writer.Flush();
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
