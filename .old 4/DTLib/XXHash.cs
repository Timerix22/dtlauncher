using System;
using System.Security.Cryptography;
namespace DTLib
{
    //
    // честно взятый с гитхаба алгоритм хеширования
    // выдаёт хеш в виде массива четырёх байтов
    //
    sealed class XXHash32 : HashAlgorithm
    {
        private const uint PRIME32_1 = 2654435761U;
        private const uint PRIME32_2 = 2246822519U;
        private const uint PRIME32_3 = 3266489917U;
        private const uint PRIME32_4 = 668265263U;
        private const uint PRIME32_5 = 374761393U;
        private static readonly Func<byte[], int, uint> FuncGetLittleEndianUInt32;
        private static readonly Func<uint, uint> FuncGetFinalHashUInt32;
        private uint _Seed32;
        private uint _ACC32_1;
        private uint _ACC32_2;
        private uint _ACC32_3;
        private uint _ACC32_4;
        private uint _Hash32;
        private int _RemainingLength;
        private long _TotalLength = 0;
        private int _CurrentIndex;
        private byte[] _CurrentArray;

        static XXHash32()
        {
            if (BitConverter.IsLittleEndian)
            {
                FuncGetLittleEndianUInt32 = new Func<byte[], int, uint>((x, i) =>
                {
                    unsafe
                    {
                        fixed (byte* array = x)
                        {
                            return *(uint*)(array + i);
                        }
                    }
                });
                FuncGetFinalHashUInt32 = new Func<uint, uint>(i => (i & 0x000000FFU) << 24 | (i & 0x0000FF00U) << 8 | (i & 0x00FF0000U) >> 8 | (i & 0xFF000000U) >> 24);
            }
            else
            {
                FuncGetLittleEndianUInt32 = new Func<byte[], int, uint>((x, i) =>
                {
                    unsafe
                    {
                        fixed (byte* array = x)
                        {
                            return (uint)(array[i++] | (array[i++] << 8) | (array[i++] << 16) | (array[i] << 24));
                        }
                    }
                });
                FuncGetFinalHashUInt32 = new Func<uint, uint>(i => i);
            }
        }

        // Creates an instance of <see cref="XXHash32"/> class by default seed(0).
        // <returns></returns>
        public static new XXHash32 Create() => new XXHash32();

        // Initializes a new instance of the <see cref="XXHash32"/> class by default seed(0).
        public XXHash32() => Initialize(0);

        // Initializes a new instance of the <see cref="XXHash32"/> class, and sets the <see cref="Seed"/> to the specified value.
        // <param name="seed">Represent the seed to be used for xxHash32 computing.</param>
        public XXHash32(uint seed) => Initialize(seed);

        // Gets the <see cref="uint"/> value of the computed hash code.
        // <exception cref="InvalidOperationException">Hash computation has not yet completed.</exception>
        public uint HashUInt32 => State == 0 ? _Hash32 : throw new InvalidOperationException("Hash computation has not yet completed.");

        // Gets or sets the value of seed used by xxHash32 algorithm.
        // <exception cref="InvalidOperationException">Hash computation has not yet completed.</exception>
        public uint Seed
        {
            get => _Seed32;
            set
            {
                if (value != _Seed32)
                {
                    if (State != 0) throw new InvalidOperationException("Hash computation has not yet completed.");
                    _Seed32 = value;
                    Initialize();
                }
            }
        }

        // Initializes this instance for new hash computing.
        public override void Initialize()
        {
            _ACC32_1 = _Seed32 + PRIME32_1 + PRIME32_2;
            _ACC32_2 = _Seed32 + PRIME32_2;
            _ACC32_3 = _Seed32 + 0;
            _ACC32_4 = _Seed32 - PRIME32_1;
        }

        // Routes data written to the object into the hash algorithm for computing the hash.
        // <param name="array">The input to compute the hash code for.</param>
        // <param name="ibStart">The offset into the byte array from which to begin using data.</param>
        // <param name="cbSize">The number of bytes in the byte array to use as data.</param>
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (State != 1) State = 1;
            var size = cbSize - ibStart;
            _RemainingLength = size & 15;
            if (cbSize >= 16)
            {
                var limit = size - _RemainingLength;
                do
                {
                    _ACC32_1 = Round32(_ACC32_1, FuncGetLittleEndianUInt32(array, ibStart));
                    ibStart += 4;
                    _ACC32_2 = Round32(_ACC32_2, FuncGetLittleEndianUInt32(array, ibStart));
                    ibStart += 4;
                    _ACC32_3 = Round32(_ACC32_3, FuncGetLittleEndianUInt32(array, ibStart));
                    ibStart += 4;
                    _ACC32_4 = Round32(_ACC32_4, FuncGetLittleEndianUInt32(array, ibStart));
                    ibStart += 4;
                } while (ibStart < limit);
            }
            _TotalLength += cbSize;
            if (_RemainingLength != 0)
            {
                _CurrentArray = array;
                _CurrentIndex = ibStart;
            }
        }

        // Finalizes the hash computation after the last data is processed by the cryptographic stream object.
        // <returns>The computed hash code.</returns>
        protected override byte[] HashFinal()
        {
            if (_TotalLength >= 16)
            {
                _Hash32 = RotateLeft32(_ACC32_1, 1) + RotateLeft32(_ACC32_2, 7) + RotateLeft32(_ACC32_3, 12) + RotateLeft32(_ACC32_4, 18);
            }
            else
            {
                _Hash32 = _Seed32 + PRIME32_5;
            }
            _Hash32 += (uint)_TotalLength;
            while (_RemainingLength >= 4)
            {
                _Hash32 = RotateLeft32(_Hash32 + FuncGetLittleEndianUInt32(_CurrentArray, _CurrentIndex) * PRIME32_3, 17) * PRIME32_4;
                _CurrentIndex += 4;
                _RemainingLength -= 4;
            }
            unsafe
            {
                fixed (byte* arrayPtr = _CurrentArray)
                {
                    while (_RemainingLength-- >= 1)
                    {
                        _Hash32 = RotateLeft32(_Hash32 + arrayPtr[_CurrentIndex++] * PRIME32_5, 11) * PRIME32_1;
                    }
                }
            }
            _Hash32 = (_Hash32 ^ (_Hash32 >> 15)) * PRIME32_2;
            _Hash32 = (_Hash32 ^ (_Hash32 >> 13)) * PRIME32_3;
            _Hash32 ^= _Hash32 >> 16;
            _TotalLength = State = 0;
            return BitConverter.GetBytes(FuncGetFinalHashUInt32(_Hash32));
        }

        private static uint Round32(uint input, uint value) => RotateLeft32(input + (value * PRIME32_2), 13) * PRIME32_1;

        private static uint RotateLeft32(uint value, int count) => (value << count) | (value >> (32 - count));

        private void Initialize(uint seed)
        {
            HashSizeValue = 32;
            _Seed32 = seed;
            Initialize();
        }
    }
}
