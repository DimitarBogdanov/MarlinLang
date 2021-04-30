/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     Utils.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

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
