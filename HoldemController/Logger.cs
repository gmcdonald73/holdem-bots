using System;
using System.IO;
using System.Collections.Generic;

using HoldemPlayerContract;

namespace HoldemController
{

    internal class Logger
    {
        private static TextWriter _logTextWriter = null;
        private static bool _writeToConsole = false;
        private static string _sLogFileName = "";

        public static void SetWriteToConsole(bool writeToConsole)
        {
            _writeToConsole = writeToConsole;
        }
        public static void SetLogFileName(string sLogFileName)
        {
            _sLogFileName = sLogFileName;
        }

        public static void Log(string sMessage, params object[] arg)
        {
            if (_logTextWriter == null)
            {
                if(_sLogFileName == "")
                {
                    throw new Exception("Logger file name not specified");
                }

                _logTextWriter = new StreamWriter(_sLogFileName, false);
            }

            if(_writeToConsole)
            {
                System.Console.WriteLine(sMessage, arg);
            }

            _logTextWriter.WriteLine(sMessage, arg);
        }

        public static void Close()
        {
            if(_logTextWriter != null)
            {
                _logTextWriter.Close();
                _logTextWriter = null;
            }
        }
    }

    public class TimingLogger
    {
        private static TextWriter _logTextWriter = null;
        private static string _sLogFileName = "";

        public static void SetLogFileName(string sLogFileName)
        {
            _sLogFileName = sLogFileName;
        }

        public static void Log(string sMessage, params object[] arg)
        {
            if (_logTextWriter == null)
            {
                if(_sLogFileName == "")
                {
                    throw new Exception("TimingLogger file name not specified");
                }

                _logTextWriter = new StreamWriter(_sLogFileName, false);
                _logTextWriter.WriteLine("Hand,Stage,PlayerNum,MethodName, MillisecondsTaken, OtherInfo1, OtherInfo2, OtherInfo3");
            }

            _logTextWriter.WriteLine(sMessage, arg);
        }

        public static void Close()
        {
            if(_logTextWriter != null)
            {
                _logTextWriter.Close();
                _logTextWriter = null;
            }
        }
    }

}
