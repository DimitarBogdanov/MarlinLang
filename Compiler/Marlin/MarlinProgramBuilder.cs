/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     MarlinProgramBuilder.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.CodeGen;
using Marlin.Lexing;
using Marlin.Optimisation;
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
            AWAITING_SEMANTIC_ANALYSIS_PASS2,
            AWAITING_CODE_OPTIMISATION,
            AWAITING_CODE_GENERATION
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
        private static bool BuildFailed
        {
            get
            {
                return CodeGenFailed || allWarnings.Any(x => x.WarningLevel == Level.ERROR);
            }
        }
        private static bool CodeGenFailed { get; set; } = false;

        private static Dictionary<string, FileBuild> fileStatuses = new();
        private static List<string> fileList = new();

        private static Node MergeTrees()
        {
            if (fileStatuses.Count == 0)
            {
                return null;
            }

            Node newMainNode = fileStatuses.First().Value.rootNode;

            for (int i = 1; i < fileStatuses.Count; i++)
            {
                foreach (Node childNode in fileStatuses.Values.ToArray()[i].rootNode.Children)
                {
                    newMainNode.AddChild(childNode);
                }
            }

            fileStatuses = null; // We don't need to clog up all of this ram!

            return newMainNode;
        }

        public static void Build()
        {
            long startTime = Utils.CurrentTimeMillis();

            Console.WriteLine("Building " + Program.SOURCE_DIR + " ...");

            Build(Program.SOURCE_DIR);
            CodeGen();

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

            Console.Write($"Build completed in {((endTime - startTime) / 1000.0).ToString().Replace(',', '.')} sec: ");
            Console.ForegroundColor = (BuildFailed ? ConsoleColor.Red : ConsoleColor.Green);
            Console.WriteLine(BuildFailed ? "FAILED" : "SUCCESSFUL");
            Console.ForegroundColor = previousColor;

            if (Program.DEBUG_MODE)
            {
                Console.WriteLine("   Of which...");
                Console.WriteLine("    Lexing:              " + MarlinTokenizer.totalTokenizationTime + " ms");
                Console.WriteLine("    Parsing:             " + MarlinParser.TotalParseTime + " ms");
                Console.WriteLine("    Semantic analysis:   ");
                Console.WriteLine("        Pass 1:          " + MarlinSemanticAnalyser.PassOneTookMs + " ms");
                Console.WriteLine("        Pass 2:          " + MarlinSemanticAnalyser.PassTwoTookMs + " ms");
                Console.WriteLine("    Code optimisation:   " + MarlinCodeOptimiser.optimisationTime + " ms");
                Console.WriteLine("    Code generation:     " + Target.TotalCodeGenTime + " ms");
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

                        if (Program.CREATE_FILE_TREE_GRAPHS)
                            Utils.GenerateImage(build.rootNode, filePath + ".png");
                    }
                    else
                    {
                        // Fatal errors discovered
                        build.status = FileBuildStatus.FAILED;
                    }

                    if (build.status == FileBuildStatus.FAILED || !Program.CREATE_FILE_TREE_GRAPHS)
                        Utils.DeleteImageIfExists(filePath + ".png");
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
                    // Start analysis
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
                    // Start analysis
                    MarlinSemanticAnalyser analyser = new(build.rootNode, filePath);
                    analyser.Pass2();
                    allWarnings.AddRange(analyser.warnings);

                    if (!analyser.warnings.Any(x => x.WarningLevel == Level.ERROR))
                    {
                        // No errors
                        build.status = FileBuildStatus.AWAITING_CODE_OPTIMISATION;
                    }
                    else
                    {
                        // Fatal errors discovered
                        build.status = FileBuildStatus.FAILED;
                    }
                }
            }

            // Code optimisation of each file
            foreach (string filePath in fileList)
            {
                // Make sure file is allowed to be in this phase
                fileStatuses.TryGetValue(filePath, out FileBuild build);
                if (build.status == FileBuildStatus.AWAITING_CODE_OPTIMISATION)
                {
                    // Start optimisation
                    MarlinCodeOptimiser optimiser = new(build.rootNode, filePath);
                    optimiser.Optimise();
                    allWarnings.AddRange(optimiser.warnings);

                    if (!optimiser.warnings.Any(x => x.WarningLevel == Level.ERROR))
                    {
                        // No errors
                        build.status = FileBuildStatus.AWAITING_CODE_GENERATION;
                    }
                    else
                    {
                        // Fatal errors discovered
                        build.status = FileBuildStatus.FAILED;
                    }
                }
            }

            // Stop! Clogging! Up! Ram!
            fileList = null;
        }

        private static void CodeGen()
        {
            //SymbolTable.Dump();
            SymbolTable.Flatten();

            foreach (var status in fileStatuses)
            {
                if (status.Value.status != FileBuildStatus.AWAITING_CODE_GENERATION)
                {
                    return;
                }
            }

            Node rootNode = MergeTrees();
            Target target;

            if (Program.TARGET == "LLVM")
                target = new LLVMSharpTarget(Program.MODULE_NAME);
            else if (Program.TARGET == "CLI")
                target = new CLITarget();
            else if (Program.TARGET == "TEST")
                target = new TestTarget();
            else
                throw new Exception("Invalid target");

            try
            {
                target.BeginTranslation(rootNode);
                target.Dump(Program.SOURCE_DIR + "\\out\\");
            }
            catch (Exception ex)
            {
                CodeGenFailed = true;
                ConsoleColor prev = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[This is a compiler bug. Please open an issue.]");
                Console.WriteLine("Target encountered an error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.ForegroundColor = prev;
            }
        }

        private static void BuildDirectory(string path)
        {
            // Collect files that we will be compiling
            foreach (string childPath in Directory.EnumerateFileSystemEntries(path))
            {
                if (!Utils.FireOrDirExists(path)) continue;

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
