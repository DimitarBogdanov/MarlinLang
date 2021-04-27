﻿/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     Program.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Parsing;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using TreeGenerator;

namespace Marlin
{
    class Program
    {
        public const string COMPILER_VERSION = "0.0.1";
        public const string MARLIN_VERSION = "0.1";

        public static bool DEBUG_MODE { get; private set; } = false;
        public static string SOURCE_DIR { get; private set; } = "";
        public static string START_CLASS { get; private set; } = "Main";

        public static void Main(string[] args)
        {
            ParseOptions(args);

            MarlinProgramBuilder.Build();
        }

        public static void GenerateImage(Node root)
        {
            TreeData.TreeDataTableDataTable table = new();
            AddChildrenRecursively(root, null, table);
            TreeBuilder builder = new(table)
            {
                BoxHeight = 45,
                BoxWidth = 160,
            };
            Image.FromStream(builder.GenerateTree("__ROOT__", ImageFormat.Png)).Save(Path.Combine(SOURCE_DIR, "compiler_source_tree.png"));
        }

        private static void AddChildrenRecursively(Node node, Node parent, TreeData.TreeDataTableDataTable table)
        {
            table.AddTreeDataTableRow(node.Id, (parent != null ? parent.Id : ""), node.Type.ToString(), node.ToString());
            foreach (Node childNode in node.Children)
                AddChildrenRecursively(childNode, node, table);
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

            DEBUG_MODE = options.HasOption("--debug");
            SOURCE_DIR = options.GetOption("--src");
            START_CLASS = options.HasOption("--startClass", true) ? options.GetOption("--startClass") : START_CLASS;

            if (!SOURCE_DIR.EndsWith(Path.DirectorySeparatorChar) && !SOURCE_DIR.EndsWith(Path.AltDirectorySeparatorChar))
                SOURCE_DIR += Path.DirectorySeparatorChar;

            if (DEBUG_MODE)
            {
                Console.WriteLine("Started Marlin compiler");
                Console.WriteLine("   Args:        " + string.Join(' ', args));
                Console.WriteLine("   Debug:       true");
                Console.WriteLine("   Source dir:  " + SOURCE_DIR);
                Console.WriteLine("   Start class: " + START_CLASS);
                Console.WriteLine();
            }
        }

        private static void ShowHelp()
        {
            ShowVersion(true);
            Console.WriteLine();
            Console.WriteLine("Usage: marlin [options]");
            Console.WriteLine();
            Console.WriteLine("Required options:");
            Console.WriteLine("  --src         The source directory.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help        Shows this prompt.");
            Console.WriteLine("  --version     Displays version information.");
            Console.WriteLine("  --startClass  The class to start from. It must have a main() function.");
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
