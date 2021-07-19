using System.Collections.Generic;
using System.IO;

namespace DTLib
{
    //
    // методы для работы с файловой системой
    //
    public static class FileWork
    {
        // записывает текст в файл и закрывает файл
        public static void Log(string logfile, string msg)
        {
            lock (new object())
            {
                if (logfile.Contains("\\")) DirCreate(logfile.Remove(logfile.LastIndexOf('\\')));
                var st = File.Open(logfile, FileMode.Append);
                var writer = new StreamWriter(st, SimpleConverter.UTF8);
                writer.Write(msg);
                writer.Close();
                st.Close();
            }
        }

        // создает папку если её не существует
        public static void DirCreate(string dir)
        {
            if (!Directory.Exists(dir))
            {
                // проверяет существование папки, в которой нужно создать dir
                if (dir.Contains("\\") && !Directory.Exists(dir.Remove(dir.LastIndexOf('\\'))))
                    DirCreate(dir.Remove(dir.LastIndexOf('\\')));
                Directory.CreateDirectory(dir);
            }
        }

        // создаёт файл, сохдаёт папки из его пути
        public static void FileCreate(string path)
        {
            if (!File.Exists(path))
            {
                if (path.Contains("\\")) DirCreate(path.Remove(path.LastIndexOf('\\')));
                using var s = File.Create(path);
                s.Close();
            }
        }

        // чтение параметров из конфига
        public static string ReadFromConfig(string configfile, string key)
        {
            lock (new object())
            {
                key += ": ";
                using var reader = new StreamReader(configfile);
                while (!reader.EndOfStream)
                {
                    string st = reader.ReadLine();
                    if (st.StartsWith(key))
                    {
                        string value = "";
                        for (int i = key.Length; i < st.Length; i++)
                        {
                            if (st[i] == '#') return value;
                            if (st[i] == '%')
                            {
                                bool stop = false;
                                string placeholder = "";
                                i++;
                                while (!stop)
                                {
                                    if (st[i] == '%')
                                    {
                                        stop = true;
                                        value += ReadFromConfig(configfile, placeholder);
                                    }
                                    else
                                    {
                                        placeholder += st[i];
                                        i++;
                                    }
                                }
                            }
                            else value += st[i];
                        }
                        reader.Close();
                        //if (value == "") throw new System.Exception($"ReadFromConfig({configfile}, {key}) error: key not found");
                        return value;
                    }
                }
                reader.Close();
                throw new System.Exception($"ReadFromConfig({configfile}, {key}) error: key not found");
            }
        }

        // копирует все файли и папки
        public static void DirCopy(string source_dir, string new_dir, bool Override)
        {
            DirCreate(new_dir);
            List<string> subdirs = new List<string>();
            List<string> files = GetAllFiles(source_dir, ref subdirs);
            for (int i = 0; i < subdirs.Count; i++)
            {
                DirCreate(subdirs[i].Replace(source_dir, new_dir));
            }
            for (int i = 0; i < files.Count; i++)
            {
                string f = files[i].Replace(source_dir, new_dir);
                File.Copy(files[i], f, Override);
                //PublicLog.Log(new string[] {"g", $"file <", "c", files[i], "b", "> have copied to <", "c", newfile, "b", ">\n'" });
            }
        }

        // копирует все файли и папки и выдаёт список конфликтующих файлов
        public static void DirCopy(string source_dir, string new_dir, bool owerwrite, out List<string> conflicts)
        {
            conflicts = new List<string>();
            var subdirs = new List<string>();
            var files = GetAllFiles(source_dir, ref subdirs);
            DirCreate(new_dir);
            for (int i = 0; i < subdirs.Count; i++)
            {
                DirCreate(subdirs[i].Replace(source_dir, new_dir));
            }
            for (int i = 0; i < files.Count; i++)
            {
                string newfile = files[i].Replace(source_dir, new_dir);
                if (File.Exists(newfile)) conflicts.Add(newfile);
                File.Copy(files[i], newfile, owerwrite);
                //PublicLog.Log(new string[] {"g", $"file <", "c", files[i], "b", "> have copied to <", "c", newfile, "b", ">\n'" });
            }
        }

        // выдает список всех файлов
        public static List<string> GetAllFiles(string dir)
        {
            List<string> all_files = new List<string>();
            string[] cur_files = Directory.GetFiles(dir);
            for (int i = 0; i < cur_files.Length; i++)
            {
                all_files.Add(cur_files[i]);
                //PublicLog.Log(new string[] { "b", "file found: <", "c", cur_files[i], "b", ">\n" });
            }
            string[] cur_subdirs = Directory.GetDirectories(dir);
            for (int i = 0; i < cur_subdirs.Length; i++)
            {
                //PublicLog.Log(new string[] { "b", "subdir found: <", "c", cur_subdirs[i], "b", ">\n" });
                all_files.AddRange(GetAllFiles(cur_subdirs[i]));
            }
            return all_files;
        }

        // выдает список всех файлов и подпапок в папке
        public static List<string> GetAllFiles(string dir, ref List<string> all_subdirs)
        {
            List<string> all_files = new List<string>();
            string[] cur_files = Directory.GetFiles(dir);
            for (int i = 0; i < cur_files.Length; i++)
            {
                all_files.Add(cur_files[i]);
                //PublicLog.Log(new string[] { "b", "file found: <", "c", cur_files[i], "b", ">\n" });
            }
            string[] cur_subdirs = Directory.GetDirectories(dir);
            for (int i = 0; i < cur_subdirs.Length; i++)
            {
                all_subdirs.Add(cur_subdirs[i]);
                //PublicLog.Log(new string[] { "b", "subdir found: <", "c", cur_subdirs[i], "b", ">\n" });
                all_files.AddRange(GetAllFiles(cur_subdirs[i], ref all_subdirs));
            }
            return all_files;
        }

        // удаляет папку со всеми подпапками и файлами
        public static void DirDelete(string dir)
        {
            var subdirs = new List<string>();
            var files = GetAllFiles(dir, ref subdirs);
            for (int i = 0; i < files.Count; i++)
                File.Delete(files[i]);
            for (int i = subdirs.Count - 1; i >= 0; i--)
                Directory.Delete(subdirs[i]);
            Directory.Delete(dir);
        }

        // вычисляет и записывает в manifest.dtsod хеши файлов из files_list.dtsod
        public static void CreateManifest(string dir)
        {
            if (!dir.EndsWith("\\")) dir += "\\";
            var dtsod = new Dtsod(File.ReadAllText(dir + "files_list.dtsod"));
            System.Text.StringBuilder manifestBuilder = new();
            Hasher hasher = new();
            for (int i = 0; i < dtsod["files"].Count; i++)
            {
                manifestBuilder.Append(dtsod["files"][i]);
                manifestBuilder.Append(": \"");
                byte[] hash = hasher.HashFile(dir + dtsod["files"][i]);
                manifestBuilder.Append(hash.HashToString());
                manifestBuilder.Append("\";\n");
            }
            File.WriteAllText(dir + "manifest.dtsod", manifestBuilder.ToString());
        }
    }
}
