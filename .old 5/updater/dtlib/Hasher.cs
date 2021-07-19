using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace updater
{
    //
    // хеширует массивы байтов алшоритмом SHA256 и файлы алгоримом XXHash32
    //
    public class Hasher
    {
        readonly HashAlgorithm sha256 = SHA256.Create();
        readonly HashAlgorithm xxh32 = XXHash32.Create();

        public Hasher() { }

        // хеш массива
        public byte[] Hash(byte[] input)
            => sha256.ComputeHash(input);

        // хеш из двух массивов
        public byte[] Hash(byte[] input, byte[] salt)
        {
            List<byte> rez = new List<byte>();
            rez.AddRange(input);
            rez.AddRange(salt);
            return sha256.ComputeHash(rez.ToArray());
        }

        // хеш двух массивов зацикленный
        public byte[] HashCycled(byte[] input, byte[] salt, ushort cycles)
        {
            for (uint i = 0; i < cycles; i++)
            {
                input = Hash(input, salt);
            }
            return input;
        }
        // хеш зацикленный
        public byte[] HashCycled(byte[] input, ushort cycles)
        {
            for (uint i = 0; i < cycles; i++)
            {
                input = Hash(input);
            }
            return input;
        }

        // хеш файла
        public byte[] HashFile(string filename)
        {
            //var then = DateTime.Now.Hour * 3600 + DateTime.Now.Minute * 60 + DateTime.Now.Second;
            var hash = xxh32.ComputeHash(File.OpenRead(filename));
            //var now = DateTime.Now.Hour * 3600 + DateTime.Now.Minute * 60 + DateTime.Now.Second;
            //PublicLog.Log($"xxh32 hash: {hash.HashToString()} time: {now - then}\n");
            return hash;
        }
    }
}
