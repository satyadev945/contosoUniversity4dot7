using System;
using System.Text;


//extends ILogger and logs error data sql
namespace ContosoUniversity.Logging
{
    public class Logger : ILogger
    {
        public void Information(string message)
        {
            Console.Out.WriteLine("INFO: " + message);
        }

        public void Information(string fmt, params object[] vars)
        {
            Console.Out.WriteLine("INFO: " + string.Format(fmt, vars));
        }

        public void Information(Exception exception, string fmt, params object[] vars)
        {
            Console.Out.WriteLine("INFO: " + FormatExceptionMessage(exception, fmt, vars));
        }

        public void Warning(string message)
        {
            Console.Out.WriteLine("WARN: " + message);
        }

        public void Warning(string fmt, params object[] vars)
        {
            Console.Out.WriteLine("WARN: " + string.Format(fmt, vars));
        }

        public void Warning(Exception exception, string fmt, params object[] vars)
        {
            Console.Out.WriteLine("WARN: " + FormatExceptionMessage(exception, fmt, vars));
        }

        public void Error(string message)
        {
            Console.Error.WriteLine("ERROR: " + message);
        }

        public void Error(string fmt, params object[] vars)
        {
            Console.Error.WriteLine("ERROR: " + string.Format(fmt, vars));
        }

        public void Error(Exception exception, string fmt, params object[] vars)
        {
            Console.Error.WriteLine("ERROR: " + FormatExceptionMessage(exception, fmt, vars));
        }

        public void TraceApi(string componentName, string method, TimeSpan timespan)
        {
            TraceApi(componentName, method, timespan, "");
        }

        public void TraceApi(string componentName, string method, TimeSpan timespan, string fmt, params object[] vars)
        {
            TraceApi(componentName, method, timespan, string.Format(fmt, vars));
        }
        public void TraceApi(string componentName, string method, TimeSpan timespan, string properties)
        {
            string message = String.Concat("Component:", componentName, ";Method:", method, ";Timespan:", timespan.ToString(), ";Properties:", properties);
            Console.Out.WriteLine("INFO: " + message);
        }

        private static string FormatExceptionMessage(Exception exception, string fmt, object[] vars)
        {
            // Simple exception formatting: for a more comprehensive version see 
            // http://code.msdn.microsoft.com/windowsazure/Fix-It-app-for-Building-cdd80df4
            var sb = new StringBuilder();
            sb.Append(string.Format(fmt, vars));
            sb.Append(" Exception: ");
            sb.Append(exception.ToString());
            return sb.ToString();
        }
    }
}