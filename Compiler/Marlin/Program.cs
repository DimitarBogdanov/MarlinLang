﻿/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     Program.cs
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
    class Program
    {
        public const string COMPILER_VERSION = "0.0.1";
        public const string MARLIN_VERSION = "0.1";

        public static bool DEBUG_MODE { get; private set; } = false;
        public static bool CREATE_FILE_TREE_GRAPHS { get; private set; } = false;
        public static string SOURCE_DIR { get; private set; } = "";
        public static string OUTPUT_DIR { get; private set; } = "";
        public static string START_CLASS { get; private set; } = "";
        public static string TARGET { get; private set; } = "LLVM";

        public static void Main(string[] args)
        {
            ParseOptions(args);

            MarlinProgramBuilder.Build();
        }

        private static void ParseOptions(string[] args)
        {
            CommandOptions options = new(args);

            if (options.HasOption("--help"))
            {
                ShowHelp();
                Environment.Exit(0);
            }
            else if (options.HasOption("--version"))
            {
                ShowVersion();
                Environment.Exit(0);
            }

            if (!options.HasOption("--src", true))
            {
                Console.WriteLine("No source directory! Run \"marlin --help\" for instructions.");
                Environment.Exit(0);
            }
            if (!options.HasOption("--output", true))
            {
                Console.WriteLine("No output directory! Run \"marlin --help\" for instructions.");
                Environment.Exit(0);
            }
            if (!options.HasOption("--name", true))
            {
                Console.WriteLine("No name! Run \"marlin --help\" for instructions.");
                Environment.Exit(0);
            }

            DEBUG_MODE = options.HasOption("--debug");
            CREATE_FILE_TREE_GRAPHS = options.HasOption("--treeGraphs");
            SOURCE_DIR = options.GetOption("--src");
            START_CLASS = options.HasOption("--startClass", true) ? options.GetOption("--startClass") : START_CLASS;

            if (!SOURCE_DIR.EndsWith(Path.DirectorySeparatorChar) && !SOURCE_DIR.EndsWith(Path.AltDirectorySeparatorChar))
                SOURCE_DIR += Path.DirectorySeparatorChar;
        }

        private static void ShowHelp()
        {
            ShowVersion(true);
            Console.WriteLine();
            Console.WriteLine("Usage: marlin [options]");
            Console.WriteLine();
            Console.WriteLine("Required options:");
            Console.WriteLine("  --src         The source directory.");
            Console.WriteLine("  --out         The directory to put the output file(s) in.");
            Console.WriteLine("  --name        The name of the executable that the compiler produces.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help        Shows this prompt.");
            Console.WriteLine("  --version     Displays version information.");
            Console.WriteLine("  --debug       Debug mode while compiling - extra output.");
        }

        private static void ShowVersion(bool omitCopyright = false)
        {
            Console.WriteLine("Marlin Compiler " + COMPILER_VERSION + " (Marlin " + MARLIN_VERSION + ")");
            if (!omitCopyright)
            {
                Console.WriteLine("(C) Copyright Dimitar Bogdanov, 2021");
            }
        }
    }
}
