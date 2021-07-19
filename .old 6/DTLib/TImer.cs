using System;
using System.Threading;

namespace DTLib
{
    // 
    // простой и понятный класс для выполнения каких-либо действий в отдельном потоке раз в некоторое время
    //
    public class Timer
    {
        Thread TimerThread;
        bool Repeat;

        // таймер сразу запускается
        public Timer(bool repeat, int delay, Action method)
        {
            Repeat = repeat;
            TimerThread = new Thread(() =>
            {
                do
                {
                    Thread.Sleep(delay);
                    method();
                } while (Repeat);
            });
            TimerThread.Start();
        }

        // завершение потока
        public void Stop()
        {
            Repeat = false;
            TimerThread.Abort();
        }
    }
}
