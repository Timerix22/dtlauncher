using System;

namespace DTLib
{
    //
    // изменение цвета текста в консоли
    //
    static public class ColoredText
    {
        // присвоение цвета тексту
        static public ConsoleColor ParseColor(string color)
        {
            switch (color)
            {
                //case "magneta":
                case "m":
                    return ConsoleColor.Magenta;
                //case "green":
                case "g":
                    return ConsoleColor.Green;
                //case "red":
                case "r":
                    return ConsoleColor.Red;
                //case "yellow":
                case "y":
                    return ConsoleColor.Yellow;
                //case "white":
                case "w":
                    return ConsoleColor.White;
                //case "blue":
                case "b":
                    return ConsoleColor.Blue;
                //case "cyan":
                case "c":
                    return ConsoleColor.Cyan;
                //case "gray":
                case "a":
                    return ConsoleColor.Gray;
                //case "black":
                case "t":
                    return ConsoleColor.Black;
                default:
                    throw new Exception("incorrect color: " + color);
            }
        }

        // вывод цветного текста
        static public void WriteColored(string[] input)
        {
            if (input.Length % 2 == 0)
            {
                string str = "";
                for (ushort i = 0; i < input.Length; i++)
                {
                    var c = ParseColor(input[i]);
                    if (Console.ForegroundColor != c)
                    {
                        Console.Write(str);
                        Console.ForegroundColor = c;
                        str = "";
                    }
                    str += input[++i];
                }
                if (str != "")
                    Console.Write(str);
            }
            else
            {
                throw new Exception("error in WriteColored(): every text string must have color string before");
            }
        }

        /*static public void WriteColoredB(string[] input)
        {
            if (input.Length % 3 == 0)
            {
                string str = "";
                for (ushort i = 0; i < input.Length; i++)
                {
                    var f = ParseColor(input[i]);
                    var b = ParseColor(input[++i]);
                    if (Console.ForegroundColor != f || Console.BackgroundColor != b)
                    {
                        Console.Write(str);
                        Console.ForegroundColor = f;
                        Console.BackgroundColor = b;
                        str = "";
                    }
                    str += input[++i];
                }
                if (str != "")
                    Console.Write(str);
            }
            else
            {
                throw new Exception("error in WriteColored(): every text string must have color string before");
            }
        }*/

        static public void WriteColored(string color, string text)
        {
            var c = ParseColor(color);
            if (Console.ForegroundColor != c)
            {
                Console.ForegroundColor = c;
            }
            Console.Write(text);
        }

        // ввод цветного текста
        static public string ReadColored(string color)
        {
            var c = ParseColor(color);
            if (Console.ForegroundColor != c)
            {
                Console.ForegroundColor = c;
            }
            string text = Console.ReadLine();
            return text;
        }
    }
}