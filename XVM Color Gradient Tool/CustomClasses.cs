using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XVMCGT
{
    public static class XVMCGTLog
    {
        public enum Type { Error, Info}
        public static List<string> LineTypes = new List<string> { "Error", "Info" };
        public static string LogFile = AppDomain.CurrentDomain.BaseDirectory + @"\log.txt";

        public static string FormattingStringFileNotFound = "Cannot find file: {0}";
        public static string Buffer = "";

        public static void CreateLogFile()
        {
            StreamWriter sw = new StreamWriter(LogFile);
            sw.Close();
        }

        public static void WriteLine(string line, Type type)
        {
            WriteLine(line, LineTypes[(int)type]);
        }
        public static void WriteLine(string line, string type)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(line))
                {
                    StreamWriter sw = File.AppendText(LogFile);
                    sw.WriteLine(String.Format("[{0}-{1}] [{2}] {3}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), type, line.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")));
                    sw.Close();
                }
            }
            catch
            {
                //WRALog.Buffer += String.Format("[{0}-{1}] [{2}] {3}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), type, line.Replace(Environment.NewLine, ""));
                //int i = 0;
                //i++;
            }
        }

        public static void WriteError(Exception excp)
        {
            WriteError("", excp);
        }
        public static void WriteError(string line, Exception excp)
        {
            WriteError(line);
            WriteError(String.Format("Source: {0}", excp.Source));

            int i = 1;


            if (excp.Message != null)
            {
                if (excp.Message.Split('\n').Length > 1)
                {
                    WriteError("Message:");

                    foreach (string s in excp.Message.Split('\n'))
                    {
                        WriteError(String.Format("{0:00}: {1}", i, s));
                        i++;
                    }
                }
                else
                    WriteError(String.Format("Message: {0}", excp.Message));
            }

            if (excp.StackTrace != null)
            {
                if (excp.StackTrace.Split('\n').Length > 1)
                {
                    WriteError("StackTrace:");

                    i = 1;

                    foreach (string s in excp.StackTrace.Split('\n'))
                    {
                        WriteError(String.Format("{0:00}: {1}", i, s));
                        i++;
                    }
                }
                else
                    WriteError(String.Format("Message: {0}", excp.StackTrace));
            }

            if (excp.TargetSite != null)
            {
                WriteError("Target Site:");
                WriteError(excp.TargetSite.ToString());
            }

            if (excp.InnerException != null)
            {
                WriteError("Inner Exception:", excp.InnerException);
            }
        }
        public static void WriteError(string line)
        {
            WriteLine(line, Type.Error);
        }
        public static void WriteInfo(string line)
        {
            WriteLine(line, Type.Info);
        }
    }
}
