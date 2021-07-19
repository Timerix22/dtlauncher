using System;

namespace DTLib
{
    //
    // вывод и ввод цветного текста в консоли
    // работает медленнее чем хотелось бы
    //
    public static class ColoredConsole
    {
        // парсит название цвета в ConsoleColor
        public static ConsoleColor ParseColor(string color)
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
                case "gray":
                    return ConsoleColor.Gray;
                //case "black":
                case "black":
                    return ConsoleColor.Black;
                default:
                    throw new Exception("incorrect color: " + color);
            }
        }

        // вывод цветного текста
        public static void Write(params string[] input)
        {
            if (input.Length == 1)
            {
                if (Console.ForegroundColor != ConsoleColor.Green) Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(input[0]);
            }
            else if (input.Length % 2 == 0)
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
                if (str != "") Console.Write(str);
            }
            else throw new Exception("error in Write(): every text string must have color string before");
        }

        // ввод цветного текста
        public static string Read(string color)
        {
            var c = ParseColor(color);
            if (Console.ForegroundColor != c) Console.ForegroundColor = c;
            return Console.ReadLine();
        }
    }
}