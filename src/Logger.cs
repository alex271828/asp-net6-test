using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNet6Test
{
    public class Logger
    {
        static System.Text.UTF8Encoding utf8enc = new System.Text.UTF8Encoding(false, true);

        public Logger(string logFolder, string filenamePrefix)
        {
            _logFolder = logFolder;

            if (!string.IsNullOrEmpty(_logFolder))
            {
                if (Directory.Exists(_logFolder))
                {
                    var nowDirString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fffffff");

                    var filename = Path.Combine(_logFolder, $"{filenamePrefix}_latest.log");

                    var filenameBak = Path.Combine(_logFolder, $"{filenamePrefix}_{nowDirString}.log");

                    if (File.Exists(filename)) File.Move(filename, filenameBak, false);

                    _logStream = File.Open(filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                }
            }
        }

        readonly string _logFolder;
        Stream _logStream;

        public void Close()
        {
            if (_logStream != null)
            {
                try
                {
                    _logStream.Close();
                    _logStream.Dispose();
                }
                finally
                {
                    _logStream = null;
                }
            }
        }

        void WriteLog(string message, IEnumerable<KeyValuePair<string, string>> properties, Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"################################ {DateTime.UtcNow.ToString("o")}");
            sb.AppendLine(message);
            sb.AppendLine();
            if (properties != null)
            {
                foreach (var item in properties)
                {
                    sb.AppendLine($"  {item.Key}={item.Value}");
                }
            }
            if (ex != null)
            {
                sb.AppendLine($"  Exception={ex.ToString()}");
            }

            sb.AppendLine();

            string messageText = sb.ToString();

            var bytes = utf8enc.GetBytes(messageText);

            if (_logStream != null)
            {
                lock (_logStream)
                {
                    _logStream.Write(bytes, 0, bytes.Length);
                    _logStream.Flush();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(messageText);
            }
        }

        public void Log(string message, IEnumerable<KeyValuePair<string, string>> properties)
        {
            WriteLog(message, properties, null);
        }

        public void Log(string message, IEnumerable<KeyValuePair<string, string>> properties, Exception ex)
        {
            WriteLog(message, properties, ex);
        }

        public void Log(string message, Exception ex)
        {
            WriteLog(message, null, ex);
        }

        public void Log(string message)
        {
            WriteLog(message, null, null);
        }
    }
}
