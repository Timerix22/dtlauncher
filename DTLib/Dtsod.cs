using System;
using System.Collections.Generic;
using System.Text;

namespace DTLib
{
    //
    // это как json но не совсем
    //
    public class Dtsod
    {
        string Text;
        public Dictionary<string, dynamic> Values { get; }
        public Dtsod(string text)
        {
            Text = text;
            Values = Parse(text);
        }

        // выдаёт Exception
        public dynamic this[string key]
        {
            get
            {
                if (TryGet(key, out dynamic value)) return value;
                else throw new Exception($"Dtsod[{key}] key not found");
            }
        }

        // не выдаёт KeyNotFoundException
        public bool TryGet(string key, out dynamic value)
        {
            try
            {
                value = Values[key];
                return true;
            }
            catch (KeyNotFoundException)
            {
                //PublicLog.Log("y", $"key {key} not found\n");
                value = null;
                return false;
            }
        }

        public override string ToString() => Text;


        Dictionary<string, dynamic> Parse(string text)
        {
            Dictionary<string, dynamic> output = new();
            StringBuilder nameStrB = new();
            StringBuilder valStrB = new();
            dynamic value = null;
            bool readValue = false;
            bool readString = false;
            bool readListElem = false;
            bool isList = false;

            dynamic StringToElse(string str)
            {
                //PublicLog.Log("m", $"StringToElse({str})\n");
                if (readString) return str;
                // bool
                if (str == "true") return true;
                else if (str == "false") return false;
                // double
                else if (str.Contains(".")) return SimpleConverter.ToDouble(str);
                // ushort, uint, ulong
                else if (str.Length > 2 && str[str.Length - 2] == 'u')
                    return str[str.Length - 1] switch
                    {
                        's' => SimpleConverter.ToUShort(str.Remove(str.Length - 2)),
                        'i' => SimpleConverter.ToUInt(str.Remove(str.Length - 2)),
                        'l' => SimpleConverter.ToULong(str.Remove(str.Length - 2)),
                        _ => throw new Exception($"ParseConfig() error: unknown data type <u{str[str.Length - 1]}>"),
                    };
                // short, int, long
                else return str[str.Length - 1] switch
                {
                    's' => SimpleConverter.ToShort(str.Remove(str.Length - 1)),
                    'l' => SimpleConverter.ToLong(str.Remove(str.Length - 1)),
                    _ => SimpleConverter.ToInt(str),
                };
            }

            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '{':
                        i++;
                        for (; text[i] != '}'; i++) valStrB.Append(text[i]);
                        value = Parse(valStrB.ToString());
                        valStrB.Clear();
                        break;
                    case '}':
                        throw new Exception("ParseConfig() error: unexpected '}' at " + i + "char");
                    case '<':
                        readString = true;
                        short balance = 1;
                        while (balance != 0)
                        {
                            i++;
                            if (text[i] == '>') balance--;
                            else if (text[i] == '<') balance++;
                            valStrB.Append(text[i]);
                        }
                        valStrB.Remove(valStrB.Length - 1, 1);
                        break;
                    case '"':
                        readString = true;
                        i++;
                        while (text[i] != '"' || text[i - 1] == '\\')
                        {
                            valStrB.Append(text[i]);
                            i++;
                        }
                        break;
                    case ':':
                        readValue = true;
                        break;
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        break;
                    case '[':
                        isList = true;
                        value = new List<dynamic>();
                        break;
                    case ',':
                    case ']':
                        if (isList) value.Add(StringToElse(valStrB.ToString()));
                        else throw new Exception($"unexpected <{text[i]}> at text[{i}]");
                        valStrB.Clear();
                        break;
                    case ';':
                        // конвертация value в нужный тип данных
                        if (!isList && valStrB.Length > 0) value = StringToElse(valStrB.ToString());
                        if (readListElem)
                        {
                            if (!output.ContainsKey(nameStrB.ToString())) output.Add(nameStrB.ToString(), new List<dynamic>());
                            output[nameStrB.ToString()].Add(value);
                        }
                        else output.Add(nameStrB.ToString(), value);
                        nameStrB.Clear();
                        valStrB.Clear();
                        value = null;
                        readValue = false;
                        readString = false;
                        readListElem = false;
                        isList = false;
                        break;
                    // коммент
                    case '#':
                        for (; i < text.Length && text[i] != '\n'; i++) ;
                        break;
                    // если $ перед названием параметра поставить, значение (value) добавится в лист с таким названием (nameStrB.ToString())
                    case '$':
                        if (nameStrB.ToString().Length != 0) throw new Exception("unexpected usage of '$' at char " + i.ToString());
                        readListElem = true;
                        break;
                    default:
                        if (readValue) valStrB.Append(text[i]);
                        else nameStrB.Append(text[i]);
                        break;
                };
            }
            return output;
        }


    }
}
