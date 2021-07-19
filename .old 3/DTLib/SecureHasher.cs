using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace DTLib
{
    //
    // создаёт хеши из массива байт, двух массивов байт, стрима
    //
    public class SecureHasher
    {
        private dynamic hasher;

        // можно указать размер хеша (по умолчанию 256 байт)
        public SecureHasher(short type)
        {
            switch (type)
            {
                case 256:
                    hasher = SHA256.Create();
                    break;
                case 384:
                    hasher = SHA384.Create();
                    break;
                case 512:
                    hasher = SHA512.Create();
                    break;
                default:
                    throw new Exception("unknown hash algorithm type: " + type + "\nin shouid be 256, 384 or 512\n");
            }
        }
        public SecureHasher()
        {
            hasher = SHA256.Create();
        }

        // просто хеш
        public byte[] Hash(byte[] input)
        {
            return hasher.ComputeHash(input);
        }

        // хеш из двух массивов
        public byte[] HashSalt(byte[] input, byte[] salt)
        {
            List<byte> rez = new List<byte>();
            rez.AddRange(input);
            rez.AddRange(salt);
            return hasher.ComputeHash(rez.ToArray());
        }

        // читает стрим и вычисляет хеш всей инфы в нём
        public byte[] HashStream(Stream st)
        {
            byte[] data = new byte[1024];
            List<byte> rez = new List<byte>();
            int offset = 0;
            while (offset < st.Length)
            {
                offset += st.Read(data, offset, 1024);
                rez.AddRange(hasher.ComputeHash(data));
            }
            return hasher.ComputeHash(rez.ToArray());
        }

        // Хеш двух массивов в цикле.
        // Работает быстро, так что вполне можно использовать количество циклов до 8к

        public byte[] HashSaltCycled(byte[] input, byte[] salt, ushort cycles)
        {
            for (uint i = 0; i < cycles; i++)
            {
                input = HashSalt(input, salt);
            }
            return input;
        }

        public byte[] HashCycled(byte[] input, ushort cycles)
        {
            for (uint i = 0; i < cycles; i++)
            {
                input = Hash(input);
            }
            return input;
        }

        public byte[] HashFile(string filename)
        {
            var md5 = MD5.Create();
            var stream = File.OpenRead(filename);
            var hash = HashSaltCycled(md5.ComputeHash(stream), filename.ToBytes(), 512);
            stream.Close();
            return hash;
        }
    }
}
