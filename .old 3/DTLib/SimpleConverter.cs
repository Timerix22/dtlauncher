using System;
using System.Collections.Generic;
using System.Text;

namespace DTLib
{
    //
    // содержит методы расширения для конвертации байт в строку и наоборот
    //
    public static class SimpleConverter
    {
        static public Encoding UTF8 = new UTF8Encoding(false);
        // байты в кодировке UTF8 в строку
        static public string ToStr(this byte[] bytes)
        {
            return UTF8.GetString(bytes);
        }
        static public string ToStr(this List<byte> bytes)
        {
            return UTF8.GetString(bytes.ToArray());
        }

        static public List<byte> ToList(this byte[] input)
        {
            var list = new List<byte>();
            list.AddRange(input);
            return list;
        }

        static public byte[] Remove(this byte[] input, int startIndex, int count)
        {
            List<byte> list = input.ToList();
            list.RemoveRange(startIndex, count);
            return list.ToArray();
        }

        // строку в байты
        static public byte[] ToBytes(this string str)
        {
            return UTF8.GetBytes(str);
        }

        // хеш в виде массива байт в строку (хеш изначально не в кодировке UTF8, так что метод выше не работает с ним)
        static public string HashToString(this byte[] hash)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            return builder.ToString();
        }

        static public bool StartsWith(this byte[] source, byte[] startsWith)
        {
            for (int i = 0; i < startsWith.Length; i++)
            {
                if (source[i] != startsWith[i])
                    return false;
            }
            return true;
        }

        static public bool StartsWith(this byte[] source, string startsWith)
            => StartsWith(source, startsWith.ToBytes());

        static public bool EndsWith(this byte[] source, byte[] endsWith)
        {
            for (int i = 0; i < endsWith.Length; i++)
            {
                if (source[source.Length - endsWith.Length + i] != endsWith[i])
                    return false;
            }
            return true;
        }
        static public bool EndsWith(this byte[] source, string endsWith)
            => EndsWith(source, endsWith.ToBytes());

        static public int Truncate(decimal number)
            => Convert.ToInt32(Math.Truncate(number));
    }
}
