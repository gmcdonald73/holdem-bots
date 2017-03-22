using System;
using System.IO;

namespace HoldemController
{
    public class FileLogger : IDisposable
    {
        private readonly TextWriter _writer;

        public FileLogger(string fileName)
        {
            _writer = new StreamWriter(fileName, false);
        }

        public void Log(string sMessage, params object[] arg)
        {
            _writer.WriteLine(sMessage, arg);
        }

        public void Dispose()
        {
            _writer.Close();
        }
    }
}