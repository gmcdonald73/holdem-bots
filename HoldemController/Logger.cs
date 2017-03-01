using System.IO;
using System.Collections.Generic;

using HoldemPlayerContract;

namespace HoldemController
{

    internal class Logger
    {
        private static readonly TextWriter _logTextWriter;
        private static bool _writeToConsole = false;

        static Logger()
        {
            if (_logTextWriter == null)
            {
                _logTextWriter = new StreamWriter("gamelog.txt", false);
            }
        }

        public static void SetWriteToConsole(bool writeToConsole)
        {
            _writeToConsole = writeToConsole;
        }

        public static void Log(string sMessage, params object[] arg)
        {
            if(_writeToConsole)
            {
                System.Console.WriteLine(sMessage, arg);
            }
            _logTextWriter.WriteLine(sMessage, arg);
        }

        public static void Close()
        {
            _logTextWriter.Close();
        }
    }

    public class TimingLogger
    {
        private static readonly TextWriter _logTextWriter;

        static TimingLogger()
        {
            if (_logTextWriter == null)
            {
                _logTextWriter = new StreamWriter("BotCallLog.csv", false);
                _logTextWriter.WriteLine("Hand,Stage,PlayerNum,MethodName, MillisecondsTaken, OtherInfo1, OtherInfo2, OtherInfo3");
            }
        }

        public static void Log(string sMessage, params object[] arg)
        {
            _logTextWriter.WriteLine(sMessage, arg);
        }

        public static void Close()
        {
            _logTextWriter.Close();
        }
    }

}
