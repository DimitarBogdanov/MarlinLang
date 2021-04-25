using Marlin.Lexing;
using Marlin.Parsing;
using System;
using System.Drawing;
using System.Drawing.Imaging;
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

            Tokenizer tokenizer = new("D:\\MarlinLang\\Tests\\HelloWorld\\someFile.mar");
            TokenStream tokenStream = tokenizer.Tokenize();
            MarlinParser parser = new(tokenStream);
            Node rootNode = parser.Parse();

            if (DEBUG_MODE)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("Tokens");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;

                // DO NOT USE TokenStream#Next()!!!!!!!!!!!!!
                // You will screw up the parser, it'll begin from the end and reach EOF immediately!
                foreach (Token token in tokenStream.GetTokens())
                {
                    Console.WriteLine(token);
                }

                Console.WriteLine();
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("Parsing");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (parser.warnings.Count != 0)
            {
                bool resumeCompilation = true;

                // Keep track of what the console fg was
                ConsoleColor previousColor = Console.ForegroundColor;

                foreach (CompilerWarning warning in parser.warnings)
                {
                    resumeCompilation = resumeCompilation && (warning.WarningLevel != CompilerWarning.Level.ERROR);
                    
                    
                    switch (warning.WarningLevel)
                    {
                        case CompilerWarning.Level.MESSAGE:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case CompilerWarning.Level.WARNING:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case CompilerWarning.Level.ERROR:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                    }

                    Console.WriteLine(warning.RootCause.line + ":" + warning.RootCause.col + " - " + warning.Message);
                }

                // Restore previous console color
                Console.ForegroundColor = previousColor;

                if (!resumeCompilation)
                {
                    Console.WriteLine("Build FAILED");
                    return;
                }
            }

            Console.WriteLine("Build SUCCEEDED");

            GenerateImage(rootNode);
        }

        public static void GenerateImage(Node root)
        {
            TreeData.TreeDataTableDataTable table = new();
            AddChildrenRecursively(root, table);
            TreeBuilder builder = new(table)
            {
                BoxHeight = 30,
                BoxWidth = 150,
            };
            Image.FromStream(builder.GenerateTree("__ROOT__", ImageFormat.Png)).Save("D:\\MarlinLang\\Tests\\HelloWorld\\tree.png");
        }

        private static void AddChildrenRecursively(Node node, TreeData.TreeDataTableDataTable table)
        {
            table.AddTreeDataTableRow(node.Id, (node.Parent != null ? node.Parent.Id : ""), node.Type.ToString(), node.ToString());
            foreach (Node childNode in node.Children)
                AddChildrenRecursively(childNode, table);
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
                return;
            }

            DEBUG_MODE = options.HasOption("--debug");
            SOURCE_DIR = options.GetOption("--src");
            START_CLASS = options.HasOption("--startClass") ? options.GetOption("--startClass") : START_CLASS;

            if (DEBUG_MODE)
            {
                Console.WriteLine("Started Marlin compiler");
                Console.WriteLine("   Args: " + string.Join(' ', args));
                Console.WriteLine("   Debug: true");
                Console.WriteLine("   Source dir: " + SOURCE_DIR);
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
