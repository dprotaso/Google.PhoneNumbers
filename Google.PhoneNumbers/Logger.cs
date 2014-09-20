using System;
using System.Diagnostics;
using System.IO;

namespace Google.PhoneNumbers
{
    enum Level
    {
        SEVERE,
        WARNING,
        INFO,
        CONFIG,
        FINE,
        FINER,
        FINEST,
    }
    class Logger
    {
        private Logger() { }
        private string TypeName { get; set; }

        public static Logger getLogger(Type type)
        {
            var logger = new Logger();
            logger.TypeName = type.Name;
            return logger;
        }

        public void log(Level warning, string message)
        {
            Debug.WriteLine(String.Format("[{0}] [{1}] {2}", TypeName, warning.ToString(), message));
        }

        public void log(Level warning, string message, Exception ioException)
        {
            Debug.WriteLine(String.Format("[{0}] [{1}] {2}", TypeName, warning.ToString(), message));
        }
    }
}
