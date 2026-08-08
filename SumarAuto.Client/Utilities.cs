using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace SumarAuto.Client
{
    public static class Utilities
    {
        private static string _contents;
        public static void SetResultMessage(string contents)
        {
            _contents = contents;

        }

        public static string GetResultMessage()
        {
            return _contents;
        }

        public static void SetResultLogMessage(string contents)
        {
            using (StreamWriter TextWriter = new StreamWriter(ConfigurationManager.AppSettings["LogFile"] + "\\" + DateTime.Now.ToString("yyyyMMdd") + "_Log.txt", true))
            {
                TextWriter.WriteLine(contents);
            }

        }
    }
}