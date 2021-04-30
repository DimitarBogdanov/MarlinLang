/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     MarlinProgramBuilder.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Lexing;
using Marlin.Parsing;
using Marlin.SemanticAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Marlin.CompilerWarning;

namespace Marlin
{
    public class MarlinProgramBuilder
    {
        enum FileBuildStatus
        {

            FAILED,

            AWAITING_TOKENIZATION,
            AWAITING_PARSING,
            AWAITING_SEMANTIC_ANALYSIS_PASS1,
            AWAITING_SEMANTIC_ANALYSIS_PASS2
        }

        class FileBuild
        {
            // File status
            public FileBuildStatus status;

            // Shared information
            public TokenStream tokenStream;
            public Node rootNode;
        }

        private readonly static List<CompilerWarning> allWarnings = new();
        private static bool buildFailed = false;

        private static readonly Dictionary<string, FileBuild> fileStatuses = new();
        private static readonly List<string> fileList = new();

        public static void Build()
        {
            long startTime = Utils.CurrentTimeMillis();

            Console.WriteLine("Building " + Program.SOURCE_DIR + " ...");

            Build(Program.SOURCE_DIR);

            Console.WriteLine();

            long endTime = Utils.CurrentTimeMillis();

            // Log messages/warns/errors
            ConsoleColor previousColor = Console.ForegroundColor;
            allWarnings.ForEach(delegate (CompilerWarning warning)
            {
                switch (warning.WarningLevel)
                {
                    case Level.MESSAGE:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case Level.WARNING:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case Level.ERROR:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }

                string friendlyFile = warning.File.Replace(Program.SOURCE_DIR, "");
                Console.WriteLine(friendlyFile + ":" + warning.RootCause.line + ":" + warning.RootCause.col + " - " + warning.Message);
            });
            Console.ForegroundColor = previousColor;

            // Build status
            if (allWarnings.Count != 0)
                Console.WriteLine();
            Console.WriteLine($"Build completed in {((endTime - startTime) / 1000.0).ToString().Replace(',', '.')} sec: " + (buildFailed ? "FAILED" : "SUCCESSFUL"));
            if (Program.DEBUG_MODE)
            {
                Console.WriteLine("    Of which...");
                Console.WriteLine("    Lexing: " + MarlinTokenizer.totalTokenizationTime + " ms");
                Console.WriteLine("    Parsing: " + MarlinParser.totalParseTime + " ms");
                Console.WriteLine("    Semantic analysis:");
                Console.WriteLine("        Pass 1: " + MarlinSemanticAnalyser.passOneTookMs + " ms");
                Console.WriteLine("        Pass 2: " + MarlinSemanticAnalyser.passTwoTookMs + " ms");
            }
        }

        private static void Build(string path)
        {
            BuildDirectory(path);

            // Tokenization of each file
            foreach (string filePath in fileList)
            {
                // Make sure file is allowed to be in this phase
                fileStatuses.TryGetValue(filePath, out FileBuild build);
                if (build.status == FileBuildStatus.AWAITING_TOKENIZATION)
                {
                    // Start tokenization
                    MarlinTokenizer tokenizer = new(filePath);
                    build.tokenStream = tokenizer.Tokenize();
                    allWarnings.AddRange(tokenizer.warnings);

                    if (!tokenizer.warnings.Any(x => x.WarningLevel == Level.ERROR))
                    {
                        // No errors
                        build.status = FileBuildStatus.AWAITING_PARSING;
                    }
                    else
                    {
                        // Fatal errors discovered
                        build.status = FileBuildStatus.FAILED;
                    }
                }
            }

            // Parsing of each file
            foreach (string filePath in fileList)
            {
                // Make sure file is allowed to be in this phase
                fileStatuses.TryGetValue(filePath, out FileBuild build);
                if (build.status == FileBuildStatus.AWAITING_PARSING)
                {
                    // Start tokenization
                    MarlinParser parser = new(build.tokenStream, filePath);
                    build.rootNode = parser.Parse();
                    allWarnings.AddRange(parser.warnings);

                    if (!parser.warnings.Any(x => x.WarningLevel == Level.ERROR))
                    {
                        // No errors
                        build.status = FileBuildStatus.AWAITING_SEMANTIC_ANALYSIS_PASS1;
                    }
                    else
                    {
                        // Fatal errors discovered
                        build.status = FileBuildStatus.FAILED;
                    }
                }
            }

            // Semantic analysis of each file
            // Pass 1
            foreach (string filePath in fileList)
            {
                Console.WriteLine("   " + filePath);
                // Make sure file is allowed to be in this phase
                fileStatuses.TryGetValue(filePath, out FileBuild build);
                if (build.status == FileBuildStatus.AWAITING_SEMANTIC_ANALYSIS_PASS1)
                {
                    // Start tokenization
                    MarlinSemanticAnalyser analyser = new(build.rootNode, filePath);
                    analyser.Pass1();
                    allWarnings.AddRange(analyser.warnings);

                    if (!analyser.warnings.Any(x => x.WarningLevel == Level.ERROR))
                    {
                        // No errors
                        build.status = FileBuildStatus.AWAITING_SEMANTIC_ANALYSIS_PASS2;
                    }
                    else
                    {
                        // Fatal errors discovered
                        build.status = FileBuildStatus.FAILED;
                    }
                }
            }

            // Semantic analysis of each file
            // Pass 2
            foreach (string filePath in fileList)
            {
                // Make sure file is allowed to be in this phase
                fileStatuses.TryGetValue(filePath, out FileBuild build);
                if (build.status == FileBuildStatus.AWAITING_SEMANTIC_ANALYSIS_PASS2)
                {
                    // Start tokenization
                    MarlinSemanticAnalyser analyser = new(build.rootNode, filePath);
                    analyser.Pass2();
                    allWarnings.AddRange(analyser.warnings);

                    if (!analyser.warnings.Any(x => x.WarningLevel == Level.ERROR))
                    {
                        // No errors
                        build.status = FileBuildStatus.AWAITING_SEMANTIC_ANALYSIS_PASS2;
                    }
                    else
                    {
                        // Fatal errors discovered
                        build.status = FileBuildStatus.FAILED;
                    }
                }
            }
        }

        private static void BuildDirectory(string path)
        {
            // Collect files that we will be compiling
            foreach (string childPath in Directory.EnumerateFileSystemEntries(path))
            {
                if (!Utils.FireOrDirExists(path)) continue;
                Console.WriteLine("   Looking at " + childPath);

                FileAttributes attributes = File.GetAttributes(childPath);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    BuildDirectory(childPath);
                }
                else if (childPath.EndsWith(".mar"))
                {
                    fileList.Add(childPath);
                    fileStatuses.Add(childPath, new()
                    {
                        status = FileBuildStatus.AWAITING_TOKENIZATION,

                        tokenStream = null,
                        rootNode = null
                    });
                }
            }
        }
    }
}
