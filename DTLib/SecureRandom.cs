using System.Security.Cryptography;

namespace DTLib
{
    //
    // Вычисление псевдослучайного числа из множества параметров. 
    // Работает медленнее чем класс System.Random, но выдаёт более случайные значения
    //
    public class SecureRandom
    {
        private RNGCryptoServiceProvider crypt = new();

        // получение массива случайных байтов
        public byte[] GenBytes(uint length)
        {
            byte[] output = new byte[length];
            crypt.GetNonZeroBytes(output);
            return output;
        }

        // получение случайного числа от 0 до 2147483647
        /*public int NextInt(uint from, int to)
        {
            int output = 0;
            int rez = 0;
            while (true)
            {
                rez = output * 10 + NextBytes(1)[0];
                if (rez < to && rez > from)
                {
                    output = rez;
                    return output;
                }
            }
        }*/
    }
}
