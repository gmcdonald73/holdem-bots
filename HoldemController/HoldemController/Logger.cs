using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HoldemController
{

    public class Logger
    {
        private static TextWriter _tw;

        static Logger()
        {

            if (_tw == null)
            {
                _tw = new StreamWriter("gamelog.txt", false);
            }
        }

        public static void Log(string sMessage, params object[] arg)
        {
            System.Console.WriteLine(sMessage, arg); 
            _tw.WriteLine(sMessage, arg);
        }

        public static void Close()
        {
            _tw.Close();
        }
    }
}
