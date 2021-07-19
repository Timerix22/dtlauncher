using System;
using System.Threading;

namespace DTLib
{
    public class Timer
    {
        public Thread TimerThread;
        bool Repeat;
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

        public void Stop()
        {
            Repeat = false;
            //throw new Exception("thread stop\n");
            TimerThread.Abort();
        }
    }
}
