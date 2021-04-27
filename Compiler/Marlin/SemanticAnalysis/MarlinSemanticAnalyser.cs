/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     MarlinSemanticAnalyser.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Parsing;
using System.Collections.Generic;

namespace Marlin.SemanticAnalysis
{
    public class MarlinSemanticAnalyser
    {
        private readonly Node rootNode;
        public readonly List<CompilerWarning> warnings = new();

        public MarlinSemanticAnalyser(Node rootNode)
        {
            this.rootNode = rootNode;
        }

        public void AddWarning(CompilerWarning warning)
        {
            warnings.Add(warning);
        }

        // Semantic work
        // Pass 1: identify all symbols from all files
        // Pass 2: identify all symbol USAGES from all files,
        //         mark any mistakes

        public bool Analyse()
        {
            bool outcome = true;

            // Pass 1
            outcome = outcome && Pass1();

            // Pass 2
            outcome = outcome && Pass2();

            // Return result
            return outcome;
        }

#pragma warning disable CS0642 // Possible mistaken empty statement
        private bool Pass1()
        {
            if (rootNode != null) ;
            return true;
        }

        private bool Pass2()
        {
            if (rootNode != null) ;
            return true;
        }
#pragma warning restore CS0642 // Possible mistaken empty statement
    }
}
