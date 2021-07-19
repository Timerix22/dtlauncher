using DTLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DTScript
{
    //
    // основной класс скриптового интерпретатора
    //
    public class ScriptRunner
    {
        // выводит текст через DTLib если дебаг включен
        public bool debug = false;
        public System.Net.Sockets.Socket mainSocket;
        internal void Debug(params string[] msg)
        {
            if (debug) PublicLog.Log(msg);
        }

        // cчитывание текста из файла и запуск выполнения
        public void RunScriptFile(string scriptfile)
        {
            Debug("g", @"----\ " + scriptfile + " /----\n");
            try { Execute(SplitScript(File.ReadAllText(scriptfile))); }
            catch (Exception e) { PublicLog.Log("r", $"dtscript RunScriptFile() error:\n{e.Message}\n{e.StackTrace}\n", "gray", " \n"); }
        }

        int globalindex = -1;
        List<Dictionary<string, object>> Storage = new();

        object GetValue(string key)
        {
            Debug("m", "GetValue(", "c", "<", "b", key, "c", ">", "m", ")\n");
            for (int i = 0; i <= globalindex; i++)
                if (Storage[i].ContainsKey(key)) return Storage[i][key];
            throw new Exception($"GetValue() exception: storage doesn't contain key<{key}>");
        }

        void SetValue(string key, object value)
        {
            Debug("m", "SetValue(", "c", "<", "b", key, "c", "> ", "c", "<", "b", value.ToString(), "c", ">", "m", ")\n");
            for (int i = 0; i <= globalindex; i++)
                if (Storage[i].ContainsKey(key))
                {
                    Storage[i][key] = value;
                    Debug(Storage[i][key].ToString());
                    return;
                }//throw new Exception($"SetValue() exception: storage alredy contains key<{key}>");
            Storage[globalindex].Add(key, value);
        }

        dynamic Execute(List<Construction> script)
        {
            Debug("y", "   executing...\n");
            // создание локального 
            globalindex++;
            List<Construction> subscript;
            Storage.Add(new Dictionary<string, object>());
            // запуск цикла
            for (int index = 0; index < script.Count; index++)
            {
                if (debug)
                {
                    PublicLog.Log(new string[] {
                        "y","\noperator: ", "m",script[index].Operator, "y"," options: ", "c", "<", "b", script[index].Options[0],  "c", ">"});
                    for (ushort n = 1; n < script[index].Options.Length; n++)
                        PublicLog.Log("w", ", ", "c", "<", "b", script[index].Options[n], "c", ">");
                    PublicLog.Log("\n");
                }
                switch (script[index].Operator)
                {
                    case "return":
                        Debug("g", "script ended");
                        return GetValue(script[index].Options[0]);
                    case "Run":
                        var proc = new Process();
                        proc.StartInfo.FileName = script[index].Options[0].Replace("\"", "");
                        proc.StartInfo.Arguments = script[index].Options[1].Replace("\"", "");
                        if (script[index].Options.Length == 3 && script[index].Options[2] == "true")
                        {
                            proc.StartInfo.CreateNoWindow = true;
                            Debug("g", $"process {script[index].Operator} started in hidden mode\n");
                        }
                        else if (script[index].Options.Length == 3 && script[index].Options[2] == "false")
                        {
                            proc.StartInfo.CreateNoWindow = false;
                            Debug("g", $"process {script[index].Operator} started in not hidden mode\n");
                        }
                        else throw new Exception("invalid arguments in Run().\n it must be: Run(string exe_file, string arguments, true/false nowindow)\n");
                        proc.StartInfo.UseShellExecute = false;
                        proc.Start();
                        proc.WaitForExit();
                        proc.Close();
                        break;
                    case "Log":
                        Debug("y", $"Log() has {script[index].Options.Length} args\n");
                        PublicLog.Log(CalcString(script[index].Options.ToList()));
                        break;
                    case "ShowFiles":
                        Debug("y", $"Log() has {script[index].Options.Length} args\n");
                        foreach (string file in Directory.GetFiles(CalcString(script[index].Options.ToList())))
                            PublicLog.Log(file + '\n');
                        break;
                    case "bool":
                        if (script[index].Options.Length > 2 && script[index].Options[1] == "=")
                        {
                            List<string> expr = new();
                            for (ushort n = 2; n < script[index].Options.Length; n++)
                                expr.Add(script[index].Options[n]);
                            // сравнение и добавление результата в storage[globalindex]
                            SetValue(script[index].Options[0], Compare(expr));
                            Debug(new string[] {"y","  bool ","b", script[index].Options[0],
                                       "w", " = ", "c", GetValue(script[index].Options[0]).ToString() + '\n'});
                        }
                        else throw new Exception("error: incorrect bool defination\n");
                        break;
                    case "num":
                        if (script[index].Options.Length > 2 && script[index].Options[1] == "=")
                        {
                            List<string> expr = new();
                            for (ushort n = 2; n < script[index].Options.Length; n++)
                            {
                                expr.Add(script[index].Options[n]);
                            }
                            SetValue(script[index].Options[0], (double)Calc(expr));
                            Debug(new string[] {"y","  num ","b", script[index].Options[0],
                                       "w", " = ", "c", GetValue(script[index].Options[0]).ToString() + '\n'});
                        }
                        else throw new Exception("Execute() error: incorrect double defination\n");
                        break;
                    case "string":
                        if (script[index].Options.Length > 2 && script[index].Options[1] == "=")
                        {
                            List<string> expr = new();
                            for (ushort n = 2; n < script[index].Options.Length; n++)
                            {
                                expr.Add(script[index].Options[n]);
                            }
                            SetValue(script[index].Options[0], CalcString(expr));
                            Debug(new string[] {"y","  string ","b", script[index].Options[0],
                                       "w", " = ", "c", GetValue(script[index].Options[0]).ToString() + '\n'});
                        }
                        break;
                    case "while":
                        if (script[index + 1].Operator != "{") throw new Exception("Execute() error: expect { after for()");
                        subscript = SplitScript(script[index + 1].Options[0]);
                        while (Compare(script[index].Options.ToList()))
                        {
                            Execute(subscript);
                        }
                        index++;
                        break;
                    case "if":
                        if (script[index + 1].Operator != "{") throw new Exception("Execute() error: expect { after for()");
                        subscript = SplitScript(script[index + 1].Options[0]);
                        if (Compare(script[index].Options.ToList()))
                        {
                            Execute(subscript);
                        }
                        index++;
                        break;
                    case "FSP_Download":
                        mainSocket.FSP_Download(script[index].Options[0], script[index].Options[1]);
                        break;
                    default:
                        throw new Exception($"Execute() error: invalid construct: {script[index].Operator}\n");
                }
            }
            Storage.RemoveAt(globalindex);
            globalindex--;
            return null;

            // операции со строками
            string CalcString(List<string> expr)
            {
                Debug("m", "CalcString(");
                foreach (string part in expr)
                    Debug("c", "<", "b", part, "c", ">");
                Debug("m", ")", "y", " started\n");
                // извлечение значений переменных
                for (ushort n = 0; n < expr.Count; n++)
                {
                    switch (expr[n][0])
                    {
                        case '+':
                            break;
                        case '"':
                            if (!expr[n].EndsWith("\"")) throw new Exception("Calc() error: invalid value <" + expr[n] + ">\n");
                            break;
                        default:
                            expr[n] = GetValue(expr[n]).ToString();
                            break;
                    }
                }
                // вычисление
                string rezult = "";
                for (ushort i = 0; i < expr.Count; i++)
                {

                    if (expr[i] == "+")
                    {
                        i++;
                        rezult += expr[i];
                    }
                    else if (i == 0) rezult = expr[0];
                    else throw new Exception($"error in Calc(): arg {expr[i]}\n");
                }
                if (rezult.Contains("\\n")) rezult = rezult.Replace("\\n", "\n");
                if (rezult.Contains("\"")) rezult = rezult.Replace("\"", "");
                Debug("y", "   returns <", "b", $"{rezult}", "y", ">\n");
                return rezult;
            }
            // операции с числами
            double Calc(List<string> expr)
            {
                Debug("m", "Calc(");
                foreach (string part in expr)
                    Debug("c", "<", "b", part, "c", ">");
                Debug("m", ")", "y", " started\n");
                // извлечение значений переменных
                for (ushort n = 0; n < expr.Count; n++)
                {
                    switch (expr[n][0])
                    {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '+':
                        case '-':
                        case '*':
                        case '/':
                            break;
                        default:
                            expr[n] = GetValue(expr[n]).ToString();
                            break;
                    }
                }
                // вычисление
                double rezult = new();
                for (ushort i = 0; i < expr.Count; i++)
                    switch (expr[i][0])
                    {
                        case '+':
                            i++;
                            rezult += Convert.ToDouble(expr[i]);
                            break;
                        case '-':
                            i++;
                            rezult -= Convert.ToDouble(expr[i]);
                            break;
                        case '*':
                            i++;
                            rezult *= Convert.ToDouble(expr[i]);
                            break;
                        case '/':
                            i++;
                            rezult /= Convert.ToDouble(expr[i]);
                            break;
                        default:
                            if (i == 0) rezult += Convert.ToDouble(expr[i]);
                            else throw new Exception($"error in Calc(): arg {expr[i]}\n");
                            break;
                    }
                Debug("y", "   returns <", "b", $"{rezult}", "y", ">\n");
                return rezult;
            }
            // сравнение
            bool Compare(List<string> expr)
            {
                Debug("m", "Compare(");
                foreach (string part in expr)
                    Debug("c", "<", "b", part, "c", ">");
                Debug("m", ")", "y", " started\n");
                // вычисление значений правой и левой части неравенства
                char act = '\0';
                double rezult_0 = new(), rezult_1;
                List<string> _expr = new List<string>();
                for (ushort n = 0; n < expr.Count; n++)
                {
                    Debug("m", $"   <{expr[n]}>\n");
                    switch (expr[n][0])
                    {
                        case '<':
                        case '>':
                            act = expr[n][0];
                            rezult_0 = Calc(_expr);
                            _expr.Clear();
                            break;
                        case '!':
                        case '=':
                            act = expr[n][0];
                            rezult_0 = Calc(_expr);
                            _expr.Clear();
                            n++;
                            break;
                        default:
                            _expr.Add(expr[n]);
                            break;
                    }
                }
                Debug("y", "   rezult_0 = <", "b", rezult_0.ToString(), "y", ">\n");
                rezult_1 = Calc(_expr);
                Debug("y", "   rezult_1 = <", "b", rezult_1.ToString(), "y", ">\n");
                Debug("y", "   act = <", "b", act.ToString(), "y", ">\n");
                bool output = act switch
                {
                    '<' => rezult_0 < rezult_1,
                    '>' => rezult_0 > rezult_1,
                    '!' => rezult_0 != rezult_1,
                    '=' => rezult_0 == rezult_1,
                    _ => throw new Exception($"error: incorrect comparsion symbol: <{act}>\n"),
                };
                Debug("y", "   return <", "c", $"{output}", "y", ">\n");
                return output;
            }
        }

        List<Construction> SplitScript(string text)
        {
            // лист для хранения обработанного текста
            List<Construction> script = new();
            string construct = "";
            string option = "";
            List<string> options = new();
            for (int index = 0; index < text.Length; index++)
            {
                switch (text[index])
                {
                    // конец распознания конструкта
                    case ';':
                        Debug(text[index].ToString() + '\n');
                        // добавление конструкта и его параметров в лист
                        if (construct != "") script.Add(new Construction(construct, options));
                        construct = "";
                        option = "";
                        options.Clear();
                        break;
                    // распознание параметров конструкта
                    case '(':
                        Debug(text[index].ToString());
                        while (text[index] != ')')
                        {
                            index++;
                            switch (text[index])
                            {
                                case '"':
                                    Debug("g", text[index].ToString());
                                    do
                                    {
                                        option += text[index];
                                        index++;
                                        Debug("g", text[index].ToString());
                                    } while (text[index] != '"');
                                    option += text[index];
                                    break;
                                case ' ':
                                case '\t':
                                case '\r':
                                case '\n':
                                    break;
                                case ')':
                                case ',':
                                    Debug(text[index].ToString());
                                    options.Add(option);
                                    option = "";
                                    break;
                                // математика
                                case '+':
                                case '-':
                                case '*':
                                case '/':
                                case '!':
                                case '=':
                                    Debug(text[index].ToString());
                                    if (option != "") options.Add(option);
                                    option = "";
                                    options.Add(text[index].ToString());
                                    break;
                                default:
                                    option += text[index];
                                    Debug("c", text[index].ToString());
                                    break;
                            }
                        }
                        if (debug && text[index + 1] == ';') PublicLog.Log("y", "\n    " + options.Count + " options have read\n");
                        break;
                    // очистка конструкта от лишних символов
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        break;
                    // 
                    case '{':
                        Debug("SplitScript() found <{>");
                        // добавление конструкта и его параметров в лист
                        if (construct != "") script.Add(new Construction(construct, options));
                        options.Clear();
                        option = "";
                        construct = "";
                        index++;
                        short bracketBalance = 1;
                        while (bracketBalance != 0)
                        {
                            if (text[index] == '{') bracketBalance++;
                            if (text[index] == '}') bracketBalance--;
                            option += text[index];
                            index++;
                        }
                        option.Remove(option.Length - 1);
                        script.Add(new Construction("{", new List<string> { option }));
                        option = "";
                        break;
                    case '}':
                        //throw new Exception($"SplitScript() error: unexpected '}}' on line {line}\n");
                        break;
                    default:
                        construct += text[index];
                        Debug("m", text[index].ToString());
                        break;
                }
            }
            // возврат листа
            return script;
        }

        class Construction
        {
            public string Operator { get; private set; }
            public string[] Options { get; private set; }
            public Construction(string oper, List<string> opts)
            {
                Operator = oper;
                Options = opts.ToArray();
            }
        }
    }
}