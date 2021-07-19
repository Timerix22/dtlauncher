using System;
using System.IO;

namespace DTLib
{
    public static class FileWork
    {
        public static void Log(string logfile, string msg)
        {
            lock (new object())
            {
                var st = File.Open(logfile, FileMode.Append);
                var writer = new StreamWriter(st, SimpleConverter.UTF8);
                string logMsg = $"[{DateTime.Now}]: {msg}";
                writer.Write(logMsg);
                writer.Close();
                st.Close();
            }
        }

        public static void DirExistenceCheck(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        static public string ReadFromConfig(string configfile, string key)
        {
            var reader = new StreamReader(configfile);
            while (!reader.EndOfStream)
            {
                string st = reader.ReadLine();
                if (!st.StartsWith("#") && st.Contains(key + ": "))
                {
                    reader.Close();
                    return st.Remove(0, st.IndexOf(key + ": ") + key.Length + 2);
                }
            }
            reader.Close();
            return null;
        }
    }
}
