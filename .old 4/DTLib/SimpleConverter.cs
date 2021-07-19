using System;
using System.Collections.Generic;
using System.Text;

namespace DTLib
{
    //
    // содержит методы расширения для различных операций и преобразований
    //
    public static class SimpleConverter
    {
        public static Encoding UTF8 = new UTF8Encoding(false);
        // байты в кодировке UTF8 в строку
        public static string ToStr(this byte[] bytes)
            => UTF8.GetString(bytes);
        public static string ToStr(this List<byte> bytes)
            => UTF8.GetString(bytes.ToArray());

        // хеш в виде массива байт в строку (хеш изначально не в кодировке UTF8, так что метод выше не работает с ним)
        public static string HashToString(this byte[] hash)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            return builder.ToString();
        }

        // строку в байты
        public static byte[] ToBytes(this string str)
            => UTF8.GetBytes(str);

        // эти методы работают как надо, в отличии от стандартных, которые иногда дуркуют
        public static bool StartsWith(this byte[] source, byte[] startsWith)
        {
            for (int i = 0; i < startsWith.Length; i++)
            {
                if (source[i] != startsWith[i]) return false;
            }
            return true;
        }

        public static bool EndsWith(this byte[] source, byte[] endsWith)
        {
            for (int i = 0; i < endsWith.Length; i++)
            {
                if (source[source.Length - endsWith.Length + i] != endsWith[i]) return false;
            }
            return true;
        }

        // Math.Truncate принимает как decimal, так и doublе,
        // из-за чего вызов метода так: Math.Truncate(10/3) выдаст ошибку "неоднозначный вызов"
        public static int Truncate(decimal number)
            => Convert.ToInt32(Math.Truncate(number));

        // сортирует в порядке возрастания элементы если это возможно, используя стандартный метод list.Sort();
        public static T[] Sort<T>(this T[] array)
        {
            var list = array.ToList();
            list.Sort();
            return list.ToArray();
        }

        // массив в лист
        public static List<T> ToList<T>(this T[] input)
        {
            var list = new List<T>();
            list.AddRange(input);
            return list;
        }

        // удаление нескольких элементов массива
        public static T[] RemoveRange<T>(this T[] input, int startIndex, int count)
        {
            List<T> list = input.ToList();
            list.RemoveRange(startIndex, count);
            return list.ToArray();
        }
        public static T[] RemoveRange<T>(this T[] input, int startIndex)
            => input.RemoveRange(startIndex, input.Length - startIndex);

        //
        public static int ToInt<T>(this T input)
            => Convert.ToInt32(input);
    }
}