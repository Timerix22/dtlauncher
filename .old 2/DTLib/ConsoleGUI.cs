using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DTLib.ConsoleGUI
{
    //
    // создание gui из текста в консоли
    //
    public class Window
    {
        public int WindowWidth { get; private set; }
        public int WindowHeight { get; private set; }
        public char[,] Text;
        public char[,] nowText;
        public char[,] TextColors;
        public char[,] nowTextColors;

        public Window(int windowWidth, int windowHeight)
        {
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
            Text = new char[windowWidth, windowHeight];
            TextColors = new char[windowWidth, windowHeight];
            nowText = new char[windowWidth, windowHeight];
            nowTextColors = new char[windowWidth, windowHeight];
            Console.WindowWidth = WindowWidth + 1;
            Console.WindowHeight = WindowHeight + 1;
            Console.BufferWidth = WindowWidth + 1;
            Console.BufferHeight = WindowHeight + 1;
            Console.OutputEncoding = SimpleConverter.UTF8;
            Console.InputEncoding = SimpleConverter.UTF8;
            Console.CursorVisible = false;
            // заполнение массивов
            for (sbyte y = 0; y < WindowHeight; y++)
            {
                for (sbyte x = 0; x < WindowWidth; x++)
                {
                    Text[x, y] = ' ';
                    nowText[x, y] = ' ';
                    TextColors[x, y] = 'w';
                    nowTextColors[x, y] = 'w';
                }
            }
        }

        // считывает массив символов из файла
        // ширина и высота текста должны быть как указанные при инициализации объекта этого класса
        public void ReadFromFile(string path)
        {
            var r = new StreamReader(path, SimpleConverter.UTF8);
            char[] s = new char[1];
            // считывание текста
            sbyte y = 0, x = 0;
            r.Read(s, 0, 1);
            while (!r.EndOfStream && y < WindowHeight)
            {
                if (x == WindowWidth)
                {
                    r.Read(s, 0, 1);
                    x = 0;
                    y++;
                }
                else
                {
                    Text[x, y] = s[0];
                    x++;
                }
                r.Read(s, 0, 1);
            }
            r.Read(s, 0, 1);
            // считывание цвета
            // если не находит цвет в файле, оставляет старый
            if (s[0] == '\n')
            {
                r.Read(s, 0, 1);
                y = 0;
                x = 0;
                while (!r.EndOfStream && y < WindowHeight)
                {
                    if (x == WindowWidth)
                    {
                        r.Read(s, 0, 1);
                        x = 0;
                        y++;
                    }
                    else
                    {
                        TextColors[x, y] = s[0];
                        x++;
                    }
                    r.Read(s, 0, 1);
                }
            }
            r.Close();
        }

        public void ResetCursor()
        {
            Console.SetCursorPosition(0, WindowHeight);
        }

        // заменяет символ выведенный, использовать после ShowAll()
        public void ChangeChar(sbyte x, sbyte y, char ch)
        {
            Text[x, y] = ch;
            nowText[x, y] = ch;
            Console.SetCursorPosition(x, y);
            ColoredText.WriteColored(TextColors[x, y].ToString(), ch.ToString());
        }

        public void ChangeColor(sbyte x, sbyte y, char color)
        {
            TextColors[x, y] = color;
            nowTextColors[x, y] = color;
            Console.SetCursorPosition(x, y);
            ColoredText.WriteColored(color.ToString(), Text[x, y].ToString());
        }

        public void ChangeCharAndColor(sbyte x, sbyte y, char color, char ch)
        {
            Text[x, y] = ch;
            nowText[x, y] = ch;
            TextColors[x, y] = color;
            nowTextColors[x, y] = color;
            Console.SetCursorPosition(x, y);
            ColoredText.WriteColored(color.ToString(), ch.ToString());
        }

        public void ChangeLine(sbyte x, sbyte y, char color, string line)
        {
            Console.SetCursorPosition(x, y);
            for (sbyte i = 0; i < line.Length; i++)
            {
                Text[x + i, y] = line[i];
                nowText[x + i, y] = line[i];
                TextColors[x + i, y] = color;
                nowTextColors[x + i, y] = color;
            }
            ColoredText.WriteColored(color.ToString(), line);
        }

        // выводит все символы
        public void ShowAll()
        {
            var l = new List<string>();
            for (sbyte y = 0; y < WindowHeight; y++)
            {
                for (sbyte x = 0; x < WindowWidth; x++)
                {
                    l.Add(TextColors[x, y].ToString());
                    l.Add(Text[x, y].ToString());
                    nowText[x, y] = Text[x, y];
                    nowTextColors[x, y] = TextColors[x, y];
                }
                l.Add("w");
                l.Add("\n");
            }
            ColoredText.WriteColored(l.ToArray());
            //Console.WriteLine();
        }

        public void UpdateAll()
        {
            for (sbyte y = 0; y < WindowHeight; y++)
            {
                for (sbyte x = 0; x < WindowWidth; x++)
                {
                    Console.SetCursorPosition(x, y);
                    if (TextColors[x, y] != nowTextColors[x, y] || Text[x, y] != nowText[x, y])
                    {
                        ColoredText.WriteColored(TextColors[x, y].ToString(), Text[x, y].ToString());
                        nowText[x, y] = Text[x, y];
                        nowTextColors[x, y] = TextColors[x, y];
                    }
                }
                Console.Write('\n');
            }
        }



        public async void ChangeCharAsync(sbyte x, sbyte y, char ch)
        {
            await Task.Run(() =>
            {
                ChangeChar(x, y, ch);
            });
        }

        public async void ChangeColorAsync(sbyte x, sbyte y, char color)
        {
            await Task.Run(() =>
            {
                ChangeColor(x, y, color);
            });
        }

        public async void ChangeCharAndColorAsync(sbyte x, sbyte y, char color, char ch)
        {
            await Task.Run(() =>
            {
                ChangeCharAndColor(x, y, color, ch);
            });
        }

        public async void ChangeLineAsync(sbyte x, sbyte y, char color, string line)
        {
            await Task.Run(() =>
            {
                ChangeLine(x, y, color, line);
            });
        }

        public async void ShowAllAsync()
        {
            await Task.Run(() =>
            {
                ShowAll();
            });
        }

        public async void UpdateAllAsync()
        {
            await Task.Run(() =>
            {
                UpdateAll();
            });
        }
    }

    public class Tab
    {
        public Window Window;
        public string Name;

        public Tab(Window window)
        {
            Window = window;
        }
    }

    public class Box
    {
        public Tab Tab;
        public int LeftTopCorner, Width, Heigth;

        public Box(Tab tab, int leftTopCorner, int width, int heigth)
        {
            Tab = tab;
            LeftTopCorner = leftTopCorner;
            Width = width;
            Heigth = heigth;
        }
    }
}
