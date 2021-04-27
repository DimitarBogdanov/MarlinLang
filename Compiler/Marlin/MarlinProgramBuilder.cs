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
        private readonly static List<CompilerWarning> allWarnings = new();
        private static bool buildFailed = false;

        public static void Build()
        {
            long startTime = Program.CurrentTimeMillis();
            
            Console.WriteLine("Building " + Program.SOURCE_DIR + " ...");
            BuildDirectory(Program.SOURCE_DIR);
            Console.WriteLine();

            long endTime = Program.CurrentTimeMillis();

            // Log messages/warns/errors
            ConsoleColor previousColor = Console.ForegroundColor;
            allWarnings.ForEach(delegate (CompilerWarning warning) {
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
        
        private static void BuildDirectory(string path)
        {
            foreach (string childPath in Directory.EnumerateFileSystemEntries(path))
            {
                if (!File.Exists(childPath)) continue;

                FileAttributes attributes = File.GetAttributes(childPath);
                if (attributes.HasFlag(FileAttributes.Directory))
                    BuildDirectory(childPath);
                else if (childPath.EndsWith(".mar"))
                    BuildFile(childPath);
            }
        }

        private static void BuildFile(string path)
        {
            Console.WriteLine("   Building file " + path + " ...");

            MarlinTokenizer tokenizer = new(path);
            TokenStream tokenStream = tokenizer.Tokenize();
            allWarnings.AddRange(tokenizer.errors);

            //
            // ORDER OF COMPILATION
            //
            //   Lexer
            // → Parser
            // → Semantical analysis

            if (tokenizer.errors.Count == 0)
            {
                // No tokenizer errors
                MarlinParser parser = new(tokenStream, path);
                Node rootNode = parser.Parse();
                allWarnings.AddRange(parser.warnings);

                if (!parser.warnings.Any(x => x.WarningLevel == Level.ERROR))
                {
                    // No parser errors
                    MarlinSemanticAnalyser analyser = new(rootNode, path);
                    analyser.Analyse();
                    allWarnings.AddRange(analyser.warnings);

                    if (!analyser.warnings.Any(x => x.WarningLevel == Level.ERROR))
                    {
                        // No analyser errors
                        // TODO
                        if (Program.CREATE_FILE_TREE_GRAPHS)
                        {
                            Program.GenerateImage(rootNode, path + ".png");
                        }
                    }
                    else
                    {
                        // Analyser had errors
                        buildFailed = true;
                        Program.DeleteImageIfExists(path + ".png"); 
                        return;
                    }
                } else
                {
                    // Parser had errors
                    buildFailed = true;
                    Program.DeleteImageIfExists(path + ".png");
                    return;
                }
            }
            else
            {
                // Tokenizer had errors
                buildFailed = true;
                Program.DeleteImageIfExists(path + ".png");
                return;
            }
        }
    }
}
