/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     SymbolTable.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using static Marlin.CompilerWarning;
using static Marlin.SemanticAnalysis.SymbolData;

namespace Marlin.SemanticAnalysis
{
    public class SymbolTable
    {
        private readonly string file;
        public static Dictionary<string, SymbolData> symbols = new();
        
        public SymbolTable(string file)
        {
            this.file = file;
        }

        static SymbolTable()
        {
            #region void and null
            symbols.Add("null", null);
            symbols.Add("void", SymbolData.Void);
            #endregion

            #region shorthands (string -> marlin.String)

            symbols.Add("string", new()
            {
                fullName = "string",
                name = "string",
                nationality = SymbolNationality.TYPE_REF,
                type = "marlin::String",
                data = new()
            });

            symbols.Add("int", new()
            {
                fullName = "int",
                name = "int",
                nationality = SymbolNationality.TYPE_REF,
                type = "marlin::Int",
                data = new()
            });

            symbols.Add("long", new()
            {
                fullName = "long",
                name = "long",
                nationality = SymbolNationality.TYPE_REF,
                type = "marlin::Long",
                data = new()
            });

            symbols.Add("boolean", new()
            {
                fullName = "boolean",
                name = "boolean",
                nationality = SymbolNationality.TYPE_REF,
                type = "marlin::Boolean",
                data = new()
            });

            #endregion

            #region stdlib

            symbols.Add("marlin", new()
            {
                fullName = "marlin",
                name = "String",
                nationality = SymbolNationality.NAMESPACE,
                type = "!",
                data = new()
            });
            
            symbols.Add("marlin::String", new()
            {
                fullName = "marlin::String",
                name = "String",
                nationality = SymbolNationality.CLASS,
                type = "marlin::String",
                data = new()
            });

            symbols.Add("marlin::Int", new()
            {
                fullName = "marlin::Int",
                name = "Int",
                nationality = SymbolNationality.CLASS,
                type = "marlin::Int",
                data = new()
            });

            symbols.Add("marlin::Long", new()
            {
                fullName = "marlin::Long",
                name = "Long",
                nationality = SymbolNationality.CLASS,
                type = "marlin::Long",
                data = new()
            });

            symbols.Add("marlin::Boolean", new()
            {
                fullName = "marlin::Boolean",
                name = "Boolean",
                nationality = SymbolNationality.CLASS,
                type = "marlin::Boolean",
                data = new()
            });

            #endregion
        }

        // Converts __global__.TestClass.main to main
        public static void Flatten()
        {
            Dictionary<string, SymbolData> newSymbols = new();

            foreach (var data in symbols)
            {
                SymbolData symbol = data.Value;

                if (symbol == null)
                {
                    newSymbols.Add(data.Key, null);
                    continue;
                }

                
                string newFullName = symbol.fullName;
                string newName = symbol.name;

                if (newFullName.StartsWith("__global__"))
                {
                    newFullName = newFullName[11..];
                }
                if (newName.StartsWith("__global__"))
                {
                    newName = newName[11..];
                }
                
                if (symbol.nationality != SymbolNationality.CLASS)
                {
                    int lastIndexOf = newFullName.LastIndexOf('.');
                    if (lastIndexOf != -1)
                        newFullName = newFullName[(lastIndexOf + 1)..];

                    lastIndexOf = newName.LastIndexOf('.');
                    if (lastIndexOf != -1)
                        newName = newName[(lastIndexOf+1)..];
                }

                symbol.fullName = newFullName;
                symbol.name = newName;


                newSymbols.Add(data.Key, symbol);
            }

            symbols = newSymbols;
        }

        public bool AddSymbol(string id, SymbolData data, Node nodeReference, MarlinSemanticAnalyser analyser)
        {
            if (symbols.ContainsKey(id))
            {
                analyser.AddWarning(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.SYMBOL_ALREADY_EXISTS,
                    message: "symbol '" + data.name + "' already exists",
                    file: file,
                    rootCause: nodeReference.Token
                ));
                return false;
            }
            else
            {
                if (data.data == null)
                    data.data = new();

                symbols.Add(id, data);
                return true;
            }
        }

        public bool UpdateSymbol(string id, SymbolData data, Node nodeReference, MarlinSemanticAnalyser analyser)
        {
            if (symbols.ContainsKey(id))
            {
                symbols.Remove(id);
                AddSymbol(id, data, nodeReference, analyser);
                return true;
            } else
            {
                analyser.AddWarning(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.UNKNOWN_SYMBOL,
                    message: "unknown symbol '" + data.name + "'" + (Program.DEBUG_MODE ? $" ({data.fullName}     {id})" : ""),
                    file: file,
                    rootCause: nodeReference.Token 
                ));
                return false;
            }
        }

        public static bool ContainsSymbol(string symbolName, string scope)
        {
            while (true)
            {
                KeyValuePair<string, SymbolData>[] arr = GetSymbolChildren(scope);
                foreach (var data in arr)
                {
                    if ((data.Value != null && data.Value.name == symbolName) || (data.Key == symbolName))
                    {
                        return true;
                    }
                }

                int lastDot = scope.LastIndexOf('.');
                if (lastDot == -1 && scope == "")
                    return false;
                else if (lastDot == -1)
                    scope = "";
                else
                    scope = scope.Remove(lastDot, scope.Length - lastDot);
            }
        }

        public static SymbolData GetSymbol(string id)
        {
            if (symbols.ContainsKey(id))
            {
                var value = symbols.GetValueOrDefault(id);
                if (value.nationality == SymbolNationality.TYPE_REF)
                {
                    return GetSymbol(value.type);
                } else
                {
                    return value;
                }
            }
            else
            {
                return null;
            }
        }

        public static SymbolData GetSymbol(string symbolName, string scope)
        {
            string originalScope = scope;
            while (true)
            {
                KeyValuePair<string, SymbolData>[] arr = GetSymbolChildren(scope);
                foreach (var data in arr)
                {
                    if ((data.Value != null && data.Value.name == symbolName) || (data.Key == symbolName))
                    {
                        if (data.Value != null)
                        {
                            if (data.Value.nationality == SymbolNationality.TYPE_REF)
                            {
                                return GetSymbol(data.Value.type, originalScope);
                            } else
                            {
                                return data.Value;
                            }
                        }
                    }
                }

                int lastDot = scope.LastIndexOf('.');
                if (lastDot == -1 && scope == "")
                {
                    return null;
                }
                else if (lastDot == -1)
                {
                    scope = "";
                }
                else
                {
                    scope = scope.Remove(lastDot, scope.Length - lastDot);
                }
            }
        }

        public static void Dump()
        {
            Console.WriteLine();
            Console.WriteLine("SYMBOL TABLE DUMP BEGIN");

            // For padding
            int longestKeyName = 5;
            foreach (var kvp in symbols)
            {
                if (kvp.Key.Length > longestKeyName)
                {
                    longestKeyName = kvp.Key.Length;
                }
            }
            longestKeyName += 4;

            // Print
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var kvp in symbols)
            {
                Console.WriteLine(kvp.Key + " ".PadRight(longestKeyName - kvp.Key.Length) + "-> " + (kvp.Value != null ? kvp.Value.ToString() : "null"));
            }

            Console.WriteLine("SYMBOL TABLE DUMP END");
        }

        private static KeyValuePair<string, SymbolData>[] GetSymbolChildren(string path)
        {
            var filtered = symbols.Where(x => x.Key.StartsWith(path)).ToArray();
            KeyValuePair<string, SymbolData>[] kvps = new KeyValuePair<string, SymbolData>[filtered.Length];
            for (int i = 0; i < filtered.Length; i++)
                kvps[i] = filtered[i];
            return kvps;
        }
    }

    public class SymbolData
    {
        public static SymbolData Void = new()
        {
            name = "void",
            nationality = SymbolNationality.SPECIAL,
            fullName = "void",
            type = "void",
            data = new()
        };

        public enum SymbolNationality
        {
            SPECIAL,
            VARIABLE,
            NAMESPACE,
            CLASS,
            ENUM,
            FUNC,
            TYPE_REF
        }

        public string name;
        public SymbolNationality nationality;
        public string type;
        public string fullName;
        public Dictionary<string, object> data;

        public override string ToString()
        {
            return $"Symbol{{name: {name}; nationality: {nationality}; type: {type}; fullName: {fullName}; data amount: {data.Count}}}";
        }
    }
}
