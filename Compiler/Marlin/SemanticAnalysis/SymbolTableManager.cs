using Marlin.Parsing;
/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     SymbolTableManager.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using System.Collections.Generic;
using static Marlin.CompilerWarning;

namespace Marlin.SemanticAnalysis
{
    public class SymbolTableManager
    {
        private readonly Dictionary<string, SymbolData> symbols = new();
        public readonly List<CompilerWarning> warnings = new();

        public void AddSymbol(string id, SymbolData data, Node nodeReference)
        {
            if (symbols.ContainsKey(id))
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.SYMBOL_ALREADY_EXISTS,
                    message: "symbol '" + data.name + "' already exists",
                    rootCause: nodeReference.Token
                ));
            }
            else
            {
                symbols.Add(id, data);
            }
        }

        public void UpdateSymbol(string id, SymbolData data, Node nodeReference)
        {
            if (symbols.ContainsKey(id))
            {
                symbols.Remove(id);
            } else
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.UNKNOWN_SYMBOL,
                    message: "unknown symbol '" + data.name + "'",
                    rootCause: nodeReference.Token 
                ));
            }
        }

        public SymbolData GetSymbol(string id)
        {
            if (symbols.ContainsKey(id))
            {
                return symbols.GetValueOrDefault(id);
            }
            else
            {
                return null;
            }
        }
    }

    public class SymbolData
    {
        public enum SymbolNationality
        {
            VARIABLE,
            CLASS,
            ENUM
        }

        public string name;
        public SymbolNationality nationality;
        public string type;
        public string fullName;
    }
}
