using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using static DTLib.PublicLog;

namespace DTLib
{
    //
    // это как json но не совсем
    //
    public class Dtsod : Dictionary<string, dynamic>
    {
        static readonly bool debug = false;

        public string Text { get; }
        //public Dictionary<string, dynamic> Values { get; set; }
        public Dtsod(string text)
        {
            Text = text;
            foreach (KeyValuePair<string, dynamic> pair in ParseNew(text))
                Add(pair.Key, pair.Value);
        }

        /*// выдаёт Exception
        public dynamic this[string key]
        {
            get
            {
                if (TryGet(key, out dynamic value)) return value;
                else throw new Exception($"Dtsod[{key}] key not found");
            }
            set
            {
                if (TrySet(key, value)) return;
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
                value = null;
                return false;
            }
        }
        public bool TrySet(string key, dynamic value)
        {
            try
            {
                Values[key] = value;
                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }*/

        public override string ToString() => Text;

        enum ValueType
        {
            List,
            Complex,
            String,
            /*Double,
            Long,
            Ulong,
            Short,
            Ushort,
            Int,
            Uint,
            Null,
            Boolean,*/
            Default
        }

        Dictionary<string, dynamic> ParseNew(string text)
        {
            Dictionary<string, dynamic> parsed = new();
            int i = 0;
            for (; i < text.Length; i++) ReadName();
            return parsed;

            void ReadName()
            {
                void ReadCommentLine()
                {
                    for (; i < text.Length && text[i] != '\n'; i++) ;
                }

                bool isListElem = false;
                dynamic value = null;
                StringBuilder defaultNameBuilder = new();

                if (debug) LogNoTime("m", "ReadName");
                for (; i < text.Length; i++)
                {
                    if (debug) LogNoTime("w", text[i].ToString());
                    switch (text[i])
                    {
                        case ' ':
                        case '\t':
                        case '\r':
                        case '\n':
                            break;
                        case ':':
                            i++;
                            value = ReadValue();
                            string name = defaultNameBuilder.ToString();
                            if (debug) LogNoTime("c", $"parsed.Add({name}, {value})\n");
                            if (isListElem)
                            {
                                if (!parsed.ContainsKey(name)) parsed.Add(name, new List<dynamic>());
                                parsed[name].Add(value);
                            }
                            else parsed.Add(name, value);
                            if (debug) LogNoTime("g", "ReadName return\n");
                            return;
                        // строка, начинающаяся с # будет считаться комментом
                        case '#':
                            ReadCommentLine();
                            break;
                        case '}':
                            throw new Exception("Parse.ReadName() error: unexpected '}' at " + i + "char");
                        // если $ перед названием параметра поставить, значение value добавится в лист с названием name
                        case '$':
                            if (defaultNameBuilder.ToString().Length != 0) throw new Exception("unexpected usage of '$' at char " + i.ToString());
                            isListElem = true;
                            break;
                        case ';':
                            throw new Exception("Parse.ReadName() error: unexpected ';' at " + i + "char");
                        default:
                            defaultNameBuilder.Append(text[i]);
                            break;
                    }
                }
            }

            dynamic ReadValue()
            {
                ValueType type = ValueType.Default;

                string ReadString()
                {
                    i++;
                    StringBuilder valueBuilder = new();
                    valueBuilder.Append('"');
                    for (; text[i] != '"' || text[i - 1] == '\\'; i++)
                    {
                        if (debug) LogNoTime("gray", text[i].ToString());
                        valueBuilder.Append(text[i]);
                    }
                    valueBuilder.Append('"');
                    if (debug) LogNoTime("gray", text[i].ToString());
                    type = ValueType.String;
                    return valueBuilder.ToString();
                }

                List<dynamic> ReadList()
                {
                    i++;
                    List<dynamic> output = new();
                    StringBuilder valueBuilder = new();
                    for (; text[i] != ']'; i++)
                    {
                        if (debug) LogNoTime("c", text[i].ToString());
                        switch (text[i])
                        {
                            case ' ':
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                            case ',':
                                output.Add(ParseValueToRightType(valueBuilder.ToString()));
                                valueBuilder.Clear();
                                break;
                            default:
                                valueBuilder.Append(text[i]);
                                break;
                        }
                    }
                    output.Add(ParseValueToRightType(valueBuilder.ToString()));
                    if (debug) LogNoTime("c", text[i].ToString());
                    type = ValueType.List;
                    return output;
                }

                Dictionary<string, dynamic> ReadComplex()
                {
                    i++;
                    StringBuilder valueBuilder = new();
                    for (; text[i] != '}'; i++)
                    {
                        if (debug) LogNoTime("y", text[i].ToString());
                        if (text[i] == '"')
                        {
                            valueBuilder.Append(ReadString());
                        }
                        else valueBuilder.Append(text[i]);
                    }
                    if (debug) LogNoTime("y", text[i].ToString());
                    type = ValueType.Complex;
                    if (debug) LogNoTime("g", valueBuilder.ToString());
                    return ParseNew(valueBuilder.ToString());
                }

                dynamic ParseValueToRightType(string stringValue)
                {

                    if (debug) LogNoTime("g", $"\nParseValueToRightType({stringValue})");
                    return stringValue switch
                    {
                        _ when stringValue.Contains('"') => stringValue.Remove(stringValue.Length - 1).Remove(0, 1),
                        // bool
                        "true" or "false" => stringValue.ToBool(),
                        // null
                        "null" => null,
                        // double
                        _ when stringValue.Contains('.') => stringValue.ToDouble(),
                        // ushort, ulong, uint
                        _ when (stringValue.Length > 2 && stringValue[stringValue.Length - 2] == 'u') => stringValue[stringValue.Length - 1] switch
                        {
                            's' => stringValue.Remove(stringValue.Length - 2).ToUShort(),
                            'i' => stringValue.Remove(stringValue.Length - 2).ToUInt(),
                            'l' => stringValue.Remove(stringValue.Length - 2).ToULong(),
                            _ => throw new Exception($"Dtsod.Parse.ReadValue() error: wrong type <u{stringValue[stringValue.Length - 1]}>")
                        },
                        // short, long, int
                        _ => stringValue[stringValue.Length - 1] switch
                        {
                            's' => stringValue.Remove(stringValue.Length - 1).ToShort(),
                            'l' => stringValue.Remove(stringValue.Length - 1).ToLong(),
                            _ => stringValue.ToInt()
                        }
                    };
                }

                dynamic value = null;
                StringBuilder defaultValueBuilder = new();
                if (debug) LogNoTime("m", "\nReadValue\n");
                for (; i < text.Length; i++)
                {
                    if (debug) LogNoTime("b", text[i].ToString());
                    switch (text[i])
                    {
                        case ' ':
                        case '\t':
                        case '\r':
                        case '\n':
                            break;
                        case '"':
                            value = ReadString();
                            break;
                        case ';':
                            if (debug) LogNoTime("g", $"\nReadValue returns type {type} value <{value}>\n");
                            return type switch
                            {
                                ValueType.List or ValueType.Complex => value,
                                ValueType.String => ParseValueToRightType(value),
                                ValueType.Default => ParseValueToRightType(defaultValueBuilder.ToString()),
                                _ => throw new Exception($"Dtlib.Parse.ReadValue() error: can't convert value to type <{type}>")
                            };
                        case '[':
                            value = ReadList();
                            break;
                        case '{':
                            value = ReadComplex();
                            break;
                        default:
                            defaultValueBuilder.Append(text[i]);
                            break;
                    }
                }
                throw new Exception("Dtsod.Parse.ReadValue error: end of text");
            }
        }


        Dictionary<string, dynamic> ParseOld(string text)
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
                if (readString) return str;
                // bool
                switch (str)
                {
                    // предустановленные значения
                    case "true": return true;
                    case "false": return false;
                    case "null": return null;
                    default:
                        // double
                        if (str.Contains(".")) return SimpleConverter.ToDouble(str);
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
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (debug) LogNoTime(text[i].ToString());

                void ReadString()
                {
                    i++;
                    while (text[i] != '"' || text[i - 1] == '\\')
                    {
                        if (debug) LogNoTime(text[i].ToString());
                        valStrB.Append(text[i]);
                        i++;
                    }
                }

                switch (text[i])
                {
                    case '{':
                        i++;
                        for (; text[i] != '}'; i++)
                        {
                            if (text[i] == '"') ReadString();
                            else valStrB.Append(text[i]);
                        }
                        value = ParseOld(valStrB.ToString());
                        valStrB.Clear();
                        break;
                    case '}':
                        throw new Exception("ParseConfig() error: unexpected '}' at " + i + "char");
                    case '"':
                        readString = true;
                        ReadString();
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
