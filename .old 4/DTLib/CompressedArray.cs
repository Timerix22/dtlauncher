using System;
using System.Collections.Generic;
using System.Threading;

namespace DTLib
{
    public class CompressedArray
    {
        public class Array1D<T> where T : IComparable<T>
        {
            byte[] Description;
            T[] Memory;

            public Array1D() { }
            public Array1D(T[] sourceArray)
            {
                CompressArray(sourceArray);
            }

            public void CompressArray(T[] sourceArray)
            {
                var listMem = new List<T>();
                var listDesc = new List<byte>();
                T prevElement = sourceArray[0];
                listMem.Add(sourceArray[0]);
                listDesc.Add(1);
                byte repeats = 1;
                for (int i = 1; i < sourceArray.Length; i++)
                {
                    if (prevElement.CompareTo(sourceArray[i]) == 0) repeats++;
                    else
                    {
                        listMem.Add(sourceArray[i]);
                        listDesc.Add(1);
                        if (repeats > 1)
                        {
                            listDesc[listDesc.Count - 2] = repeats;
                            repeats = 1;
                        }
                    }
                    prevElement = sourceArray[i];
                }
                Memory = listMem.ToArray();
                Description = listDesc.ToArray();
                ColoredConsole.Write("b", "listMem.Count: ", "c", listMem.Count.ToString(), "b", "  listDesc.Count: ", "c", listDesc.Count + "\n");
                for (short i = 0; i < listDesc.Count; i++)
                {
                    ColoredConsole.Write("y", $"{Description[i]}:{Memory[i]}\n");
                }
            }

            // блокирует обращение к памяти из нескольких потоков
            Mutex storageUsing = new Mutex();

            // возвращает элемент по индексу так, как если бы шло обращение к обычном массиву
            public T GetElement(int index)
            {
                storageUsing.WaitOne();
                T output = default;
                int sum = 0;
                for (int i = 0; i < Description.Length; i++)
                {
                    if (sum < index) sum += Description[i];
                    else if (sum == index) output = Memory[i];
                    else output = Memory[i - 1];
                }
                storageUsing.ReleaseMutex();
                return output;
            }
        }
    }
}
