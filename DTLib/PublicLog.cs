namespace DTLib
{
    //
    // вывод логов со всех классов в библиотеке
    //
    public static class PublicLog
    {
        public delegate void LogDelegate(params string[] msg);
        // вот к этому объекту подключайте методы для вывода логов
        public static LogDelegate Log;
        public static LogDelegate LogNoTime;
        public static LogDelegate FSP_DownloadSpeed;
    }
}
