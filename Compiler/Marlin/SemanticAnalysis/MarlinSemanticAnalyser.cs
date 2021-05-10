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
        private readonly SymbolTable symbolTable;
        public readonly List<CompilerWarning> warnings;
        private static long passOneTookMs = 0;
        private static long passTwoTookMs = 0;

        public static long PassOneTookMs { get => passOneTookMs; set => passOneTookMs = value; }
        public static long PassTwoTookMs { get => passTwoTookMs; set => passTwoTookMs = value; }

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
        // Pass 2: understand types, check correctness of code

        public void Pass1()
        {
            long start = Utils.CurrentTimeMillis();

            PassOneVisitor visitor = new(symbolTable, this);
            visitor.Visit(rootNode);

            PassOneTookMs += Utils.CurrentTimeMillis() - start;
        }

        public void Pass2()
        {
            long start = Utils.CurrentTimeMillis();

            PassTwoVisitor visitor = new(symbolTable, this, file);
            visitor.Visit(rootNode);

            PassTwoTookMs += Utils.CurrentTimeMillis() - start;
        }
    }
}
