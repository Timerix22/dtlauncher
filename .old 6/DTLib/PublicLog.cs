namespace DTLib
{
    //
    // вывод логов со всех классов в библиотеке
    //
    public static class PublicLog
    {
        public delegate void LogDelegate(string[] msg);
        // вот к этому объекту подключайте методы для вывода логов
        public static LogDelegate LogDel;

        // этот метод вызывается в библиотеке
        public static void Log(params string[] msg)
            => LogDel(msg);
    }
}
