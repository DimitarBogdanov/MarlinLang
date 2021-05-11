/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     PassTwoVisitor.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Parsing;
using System;
using System.Collections.Generic;
using static Marlin.CompilerWarning;
using static Marlin.SemanticAnalysis.SymbolData;

namespace Marlin.SemanticAnalysis
{
    public class PassTwoVisitor : IVisitor
    {
        private readonly SymbolTable symbolTable;
        private readonly MarlinSemanticAnalyser analyser;

        private readonly string file = "";
        
        private string currentSymbolPath = "__global__";
        private SymbolData currentFunctionSymbolData = null;

        public PassTwoVisitor(SymbolTable symbolTable, MarlinSemanticAnalyser analyser, string file)
        {
            this.symbolTable = symbolTable;
            this.analyser = analyser;
            this.file = file;
        }

        private string GetNodeType(Node node)
        {
            switch (node.Type)
            {
                case NodeType.BINARY_OPERATOR:
                    return GetNodeType(((BinaryOperatorNode)node).Left);

                case NodeType.FUNCTION_CALL:
                    {
                        SymbolData data = SymbolTable.GetSymbol(((FuncCallNode)node).Name, currentSymbolPath);
                        if (data != null)
                        {
                            return data.type;
                        }
                        else
                        {
                            throw new Exception("Asked to get type of node whose symbol doesn't exist");
                        }
                    }

                case NodeType.CLASS_TEMPLATE:
                    {
                        SymbolData data = SymbolTable.GetSymbol(((ClassTemplateNode)node).Name, currentSymbolPath);
                        if (data != null)
                        {
                            return data.type;
                        }
                        else
                        {
                            throw new Exception("Asked to get type of node whose symbol doesn't exist");
                        }
                    }
                case NodeType.NAME_REFERENCE:
                    {
                        SymbolData data = SymbolTable.GetSymbol(((NameReferenceNode)node).Name, currentSymbolPath);
                        if (data != null)
                        {
                            return data.type;
                        }
                        else
                        {
                            analyser.AddWarning(new(
                                level: Level.ERROR,
                                source: Source.SEMANTIC_ANALYSIS,
                                code: ErrorCode.UNKNOWN_SYMBOL,
                                message: "unknown symbol '" + ((NameReferenceNode)node).Name + "'",
                                file: file,
                                rootCause: node.Token
                            ));
                            return "?";
                        }
                    }
                case NodeType.NUMBER_INT:
                    return "marlin.Int";
                case NodeType.NUMBER_DBL:
                    return "marlin.Double";
                case NodeType.STRING:
                    return "marlin.String";
                case NodeType.BOOLEAN:
                    return "marlin.Boolean";

                default:
                    throw new Exception("Asked to get type of " + node.Type);
            }
        }

        public void Visit(Node node)
        {
            switch (node.Type)
            {
                case NodeType.BLOCK:
                    VisitBlock(node);
                    break;
                case NodeType.BINARY_OPERATOR:
                    VisitBinaryOperator((BinaryOperatorNode)node);
                    break;
                case NodeType.CLASS_TEMPLATE:
                    VisitClassTemplate((ClassTemplateNode)node);
                    break;
                case NodeType.FUNCTION:
                    VisitFunc((FuncNode)node);
                    break;
                case NodeType.FUNCTION_CALL:
                    VisitFuncCall((FuncCallNode)node);
                    break;
                case NodeType.VARIABLE_ASSIGNMENT:
                    VisitVarAssign((VarAssignNode)node);
                    break;
                case NodeType.VARIABLE_DECLARATION:
                    VisitVarDeclare((VarDeclareNode)node);
                    break;
                case NodeType.NAME_REFERENCE:
                    VisitNameReference((NameReferenceNode)node);
                    break;
                case NodeType.NUMBER_INT:
                    VisitInteger((NumberIntegerNode)node);
                    break;
                case NodeType.NUMBER_DBL:
                    VisitDouble((NumberDoubleNode)node);
                    break;
                case NodeType.STRING:
                    VisitString((StringNode)node);
                    break;
                case NodeType.BOOLEAN:
                    VisitBoolean((BooleanNode)node);
                    break;
                case NodeType.RETURN_STATEMENT:
                    VisitReturn((ReturnNode)node);
                    break;
                default:
                    throw new NotImplementedException(node.Type.ToString());
            }
        }
        public void VisitBlock(Node node)
        {
            foreach (Node child in node.Children)
            {
                Visit(child);
            }

            return;
        }
               
        public void VisitBinaryOperator(BinaryOperatorNode node)
        {
            // Just in case
            Visit(node.Left);
            Visit(node.Right);

            return;
        }
               
        public void VisitClassTemplate(ClassTemplateNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;
            VisitBlock(node);
            currentSymbolPath = currentScope;

            return;
        }
               
        public void VisitFunc(FuncNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;

            node.Name = currentSymbolPath;

            string path = currentSymbolPath;
            SymbolData data = SymbolTable.GetSymbol(path, currentSymbolPath);
            if (data == null)
            {
                return;
            }

            SymbolData typeData = SymbolTable.GetSymbol(data.type, currentSymbolPath);
            if (typeData == null)
            {
                return;
            }

            // Update regular data
            symbolTable.UpdateSymbol(path, new()
            {
                fullName = data.fullName,
                name = data.name,
                nationality = data.nationality,
                type = typeData.fullName,
                data = data.data
            }, node, analyser);

            SymbolData oldFuncSD = currentFunctionSymbolData;
            currentFunctionSymbolData = SymbolTable.GetSymbol(path, currentSymbolPath);

            VisitBlock(node);

            currentFunctionSymbolData = oldFuncSD;

            currentSymbolPath = currentScope;

            return;
        }
               
        public void VisitVarDeclare(VarDeclareNode node)
        {
            // Check var type
            string path = currentSymbolPath + "." + node.Name;
            SymbolData data = SymbolTable.GetSymbol(path);
            if (data == null)
                return;

            SymbolData typeData = SymbolTable.GetSymbol(data.type);
            if (typeData == null)
                return;

            // Update regular data
            symbolTable.UpdateSymbol(path, new()
            {
                fullName = data.fullName,
                name = data.name,
                nationality = data.nationality,
                type = typeData.type,
                data = data.data
            }, node, analyser);

            return;
        }
               
        public void VisitFuncCall(FuncCallNode node)
        {
            if (!node.Name.Contains('.'))
            {
                // Accessible from this scope
                if (SymbolTable.ContainsSymbol(node.Name, currentSymbolPath))
                {
                    SymbolData data = SymbolTable.GetSymbol(node.Name, currentSymbolPath);
                    // Check arg counts match
                    Dictionary<NameReferenceNode, NameReferenceNode> remoteArgs = (Dictionary<NameReferenceNode, NameReferenceNode>) data.data["args"];
                    List<Node> localArgs = node.Args;
                    if (remoteArgs.Count != localArgs.Count)
                    {
                        analyser.AddWarning(new(
                            level: Level.ERROR,
                            source: Source.SEMANTIC_ANALYSIS,
                            code: ErrorCode.ARGUMENT_MISMATCH,
                            message: "argument mismatch: expected " + remoteArgs.Count + " args, got " + localArgs.Count,
                            file: file,
                            rootCause: node.Token
                        ));
                    }

                    node.Name = data.fullName;

                    // Fix argument types
                    int i = 0;
                    foreach (var kvp in remoteArgs)
                    {
                        SymbolData smb = SymbolTable.GetSymbol(kvp.Key.Name, currentSymbolPath);
                        string remoteType = smb.type;

                        SymbolData varTypeData = SymbolTable.GetSymbol(remoteType);
                        remoteType = varTypeData.fullName;

                        symbolTable.UpdateSymbol(kvp.Key.Name, new()
                        {
                            fullName = smb.fullName,
                            name = varTypeData.name,
                            nationality = varTypeData.nationality,
                            type = remoteType,
                            data = varTypeData.data
                        }, node, analyser);

                        try
                        {
                            if (localArgs[i] is NameReferenceNode nameRef)
                            {
                                if (nameRef.Name == "null")
                                {
                                    continue;
                                }
                                else if (nameRef.Name == "void")
                                {
                                    analyser.AddWarning(new(
                                        level: Level.ERROR,
                                        source: Source.SEMANTIC_ANALYSIS,
                                        code: ErrorCode.VOID_MISUSE,
                                        message: "cannot use void in this context",
                                        file: file,
                                        rootCause: nameRef.Token
                                    ));
                                }
                            }
                        } catch (Exception)
                        {
                            analyser.AddWarning(new(
                                level: Level.ERROR,
                                source: Source.SEMANTIC_ANALYSIS,
                                code: ErrorCode.ARGUMENT_MISMATCH,
                                message: "argument mismatch: expected " + remoteArgs.Count + " args, got " + localArgs.Count,
                                file: file,
                                rootCause: node.Token
                            ));
                            break;
                        }

                        string localType = SymbolTable.GetSymbol(localArgs[i].StringType, currentSymbolPath).fullName;

                        if (remoteType != localType)
                        {
                            analyser.AddWarning(new(
                                level: Level.ERROR,
                                source: Source.SEMANTIC_ANALYSIS,
                                code: ErrorCode.ARGUMENT_MISMATCH,
                                message: "argument mismatch for argument " + (i + 1) + ": expected " + remoteType + ", got " + localType,
                                file: file,
                                rootCause: node.Token
                            ));
                        }

                        i++;
                    }
                } else
                {
                    analyser.AddWarning(new(
                        level: Level.ERROR,
                        source: Source.SEMANTIC_ANALYSIS,
                        code: ErrorCode.UNKNOWN_SYMBOL,
                        message: "unknown symbol '" + node.Name + "'" + (Program.DEBUG_MODE ? $" ({currentSymbolPath})" : ""),
                        file: file,
                        rootCause: node.Token
                    ));
                }
            }
            else
            {
                // Check if all symbols exist
                foreach (string path in node.Name.Split('.'))
                {
                    if (!SymbolTable.ContainsSymbol(path, currentSymbolPath))
                    {
                        analyser.AddWarning(new(
                            level: Level.ERROR,
                            source: Source.SEMANTIC_ANALYSIS,
                            code: ErrorCode.UNKNOWN_SYMBOL,
                            message: "unknown symbol '" + path + "'" + (Program.DEBUG_MODE ? $" ({node.Name})" : ""),
                            file: file,
                            rootCause: node.Token
                        ));
                        return;
                    }
                }
            }

            return;
        }

        public void VisitReturn(ReturnNode node)
        {
            string valueType = GetNodeType(node.Value);
            SymbolData smb = SymbolTable.GetSymbol(valueType, currentSymbolPath);

            if (smb == null)
            {
                return;
            }

            valueType = smb.type;

            if (currentFunctionSymbolData.type != valueType)
            {
                analyser.AddWarning(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.FUNC_RETURN_TYPE_MISMATCH,
                    message: "return statement does not match the type of the function - expected " + currentFunctionSymbolData.type + ", got " + valueType,
                    file: file,
                    rootCause: node.Token
                ));
            }

            return;
        }
               
        public void VisitVarAssign(VarAssignNode node)
        {
            string variableType = GetNodeType(node.Name);
            string valueType = GetNodeType(node.Value);
            if (variableType != valueType)
            {
                analyser.AddWarning(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.VARASSIGN_TYPE_MISMATCH,
                    message: "value type doesn't match variable type",
                    file: file,
                    rootCause: node.Token
                ));
            }

            return;
        }
               
        public void VisitNameReference(NameReferenceNode node)
        {
            if (!SymbolTable.ContainsSymbol(node.Name, currentSymbolPath))
            {
                analyser.AddWarning(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.UNKNOWN_SYMBOL,
                    message: "unknown symbol '" + node.Name + "'",
                    file: file,
                    rootCause: node.Token
                ));
            }

            return;
        }
               
        public void VisitBoolean(BooleanNode node)
        {
            return; // No need for pass 2
        }
               
        public void VisitDouble(NumberDoubleNode node)
        {
            return; // No need for pass 2
        }
               
        public void VisitInteger(NumberIntegerNode node)
        {
            return; // No need for pass 2
        }
               
        public void VisitString(StringNode node)
        {
            return; // No need for pass 2
        }
    }
}
