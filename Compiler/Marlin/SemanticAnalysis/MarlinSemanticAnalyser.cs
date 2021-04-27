/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     MarlinSemanticAnalyser.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using System;
using Marlin.Parsing;
using System.Collections.Generic;

namespace Marlin.SemanticAnalysis
{
    public class MarlinSemanticAnalyser
    {
        private readonly Node rootNode;
        private readonly string file;
        private readonly SymbolTableManager symbolTable;
        public readonly List<CompilerWarning> warnings;
        public static long passOneTookMs = 0;
        public static long passTwoTookMs = 0;

        public MarlinSemanticAnalyser(Node rootNode, string file)
        {
            this.rootNode = rootNode;
            this.file = file;
            symbolTable = new(file);
            warnings = new();
        }

        public void AddWarning(CompilerWarning warning)
        {
            warnings.Add(warning);
        }

        // Semantic work
        // Pass 1: identify all symbols from all files
        //         fill symbol table with keys and types for variables
        // Pass 2: identify all symbol USAGES from all files,
        //         fill symbol table with values,
        //         mark any mistakes

        public bool Analyse()
        {
            long start;

            // Pass 1
            start = Program.CurrentTimeMillis();
            Pass1();
            passOneTookMs += Program.CurrentTimeMillis() - start;

            // Pass 2
            start = Program.CurrentTimeMillis();
            bool outcome = Pass2();
            passTwoTookMs += Program.CurrentTimeMillis() - start;

            if (Program.DEBUG_MODE)
            {
                Console.WriteLine();
                Console.WriteLine("Symbol table dump BEGIN");
                // p.s. VS thinks the null check can be simplified. no, it can't
#pragma warning disable IDE0029
                foreach (var kvp in SymbolTableManager.symbols)
                {
                    Console.WriteLine("  " + kvp.Key + ": " + (kvp.Value == null ? "null" : kvp.Value));
                }
#pragma warning restore IDE0029
                Console.WriteLine("Symbol table dump END");
            }

            // Return result
            return outcome;
        }

        private bool Pass1()
        {
            PassOneVisitor visitor = new(symbolTable, this);
            visitor.Visit(rootNode);
            return true;
        }

        private bool Pass2()
        {
            PassTwoVisitor visitor = new(symbolTable, this, file);
            visitor.Visit(rootNode);
            return visitor.success;
        }
    }
}
