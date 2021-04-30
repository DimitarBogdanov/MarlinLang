using System;
using System.IO;

namespace Marlin
{
    public class Utils
    {
        public static long CurrentTimeMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static bool FireOrDirExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }
    }
}
