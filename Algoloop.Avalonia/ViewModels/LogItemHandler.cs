using QuantConnect.Logging;
using System;
using System.IO;

namespace Algoloop.Wpf.ViewModels
{
    public class LogItemHandler : ILogHandler
    {
        private readonly string _logFilePath;
        private readonly StreamWriter? _writer;

        public LogItemHandler(string logFile)
        {
            _logFilePath = logFile;
            try
            {
                _writer = new StreamWriter(_logFilePath, append: true) { AutoFlush = true };
            }
            catch
            {
                _writer = null;
            }
        }

        public void Debug(string text)
        {
            _writer?.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}");
        }

        public void Error(string text)
        {
            _writer?.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}");
        }

        public void Trace(string text)
        {
            _writer?.WriteLine($"[TRACE] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}");
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
