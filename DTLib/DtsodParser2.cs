using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTLib
{
    class DtsodParser2
    {

        enum ValueType
        {
            List,
            Complex,
            String,
            Double,
            Long,
            Ulong,
            Short,
            Ushort,
            Int,
            Uint,
            Null,
            Boolean,
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
                bool isListElem = false;

                void ReadCommentLine()
                {
                    for (; i < text.Length && text[i] != '\n'; i++) ;
                }

                dynamic value = null;
                StringBuilder defaultNameBuilder = new();

                for (; i < text.Length; i++)
                {
                    switch (text[i])
                    {
                        case ' ':
                        case '\t':
                        case '\r':
                        case '\n':
                            break;
                        case ':':
                            value = ReadValue();
                            break;
                        // строка, начинающаяся с # будет считаться комментом
                        case '#':
                            ReadCommentLine();
                            break;
                        case '}':
                            throw new Exception("ParseConfig() error: unexpected '}' at " + i + "char");
                        // если $ перед названием параметра поставить, значение value добавится в лист с названием name
                        case '$':
                            if (defaultNameBuilder.ToString().Length != 0) throw new Exception("unexpected usage of '$' at char " + i.ToString());
                            isListElem = true;
                            break;
                        case ';':
                            string name = defaultNameBuilder.ToString();
                            if (isListElem)
                            {
                                if (!parsed.ContainsKey(name)) parsed.Add(name, new List<dynamic>());
                                parsed[name].Add(value);
                            }
                            else parsed.Add(name, value);
                            return;
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
                    for (; text[i] != '"' || text[i - 1] == '\\'; i++)
                        valueBuilder.Append(text[i]);
                    type = ValueType.String;
                    return valueBuilder.ToString();
                }

                List<string> ReadList()
                {
                    List<string> output = new();
                    StringBuilder valueBuilder = new();
                    for (; text[i] != ']'; i++)
                    {
                        switch (text[i])
                        {
                            case ' ':
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                            case ',':
                                output.Add(valueBuilder.ToString());
                                break;
                            default:
                                valueBuilder.Append(text[i]);
                                break;
                        }
                    }
                    type = ValueType.List;
                    return output;
                }

                Dictionary<string, dynamic> ReadComplex()
                {
                    i++;
                    StringBuilder valueBuilder = new();
                    for (; text[i] != '}'; i++)
                    {
                        if (text[i] == '"') valueBuilder.Append(ReadString());
                        else valueBuilder.Append(text[i]);
                    }
                    type = ValueType.Complex;
                    return ParseNew(valueBuilder.ToString());
                }

                dynamic value = null;
                StringBuilder defaultValueBuilder = new();
                for (; i < text.Length; i++)
                {
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
                            if (type == ValueType.Default)
                            {
                                string valueString = defaultValueBuilder.ToString();
                                type = valueString switch
                                {
                                    "true" or "false" => ValueType.Boolean,
                                    "null" => ValueType.Null,
                                    _ when valueString.Contains('.') => ValueType.Null,
                                    _ when (valueString.Length > 2 && valueString[valueString.Length - 2] == 'u') => valueString[valueString.Length - 1] switch
                                    {
                                        's' => ValueType.Ushort,
                                        'i' => ValueType.Uint,
                                        'l' => ValueType.Ulong,
                                        _ => throw new Exception($"Dtsod.Parse.ReadValue() error: wrong type <u{valueString[valueString.Length - 1]}>")
                                    },
                                    _ => valueString[valueString.Length - 1] switch
                                    {
                                        's' => ValueType.Short,
                                        'l' => ValueType.Long,
                                        _ => ValueType.Int
                                    }
                                };
                            }

                            return type switch
                            {
                                ValueType.String or ValueType.List or ValueType.Complex => value,
                                ValueType.Double => SimpleConverter.ToDouble(value),
                                ValueType.Long => SimpleConverter.ToLong(value.Remove(value.Length - 1)),
                                ValueType.Ulong => SimpleConverter.ToULong(value.Remove(value.Length - 2)),
                                ValueType.Short => SimpleConverter.ToShort(value.Remove(value.Length - 1)),
                                ValueType.Ushort => SimpleConverter.ToUShort(value.Remove(value.Length - 2)),
                                ValueType.Int => SimpleConverter.ToInt(value),
                                ValueType.Uint => SimpleConverter.ToUInt(value),
                                ValueType.Boolean => SimpleConverter.ToBool(value),
                                ValueType.Null => null,
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
    }
}
