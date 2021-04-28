using Marlin.Parsing;
using System;
using System.Collections.Generic;
using static Marlin.CompilerWarning;
using static Marlin.SemanticAnalysis.SymbolData;

namespace Marlin.SemanticAnalysis
{
    public class PassTwoVisitor : IVisitor
    {
        private readonly SymbolTableManager symbolTable;
        private readonly MarlinSemanticAnalyser analyser;

        private readonly string file = "";
        
        private string currentSymbolPath = "__global__";
        public bool success = true;

        public PassTwoVisitor(SymbolTableManager symbolTable, MarlinSemanticAnalyser analyser, string file)
        {
            this.symbolTable = symbolTable;
            this.analyser = analyser;
            this.file = file;
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
                default:
                    throw new NotImplementedException();
            };
        }

        public void VisitBlock(Node node)
        {
            foreach (Node child in node.Children)
            {
                Visit(child);
            }
        }

        public void VisitBinaryOperator(BinaryOperatorNode node)
        {
            // Just in case
            Visit(node.Left);
            Visit(node.Right);
        }

        public void VisitClassTemplate(ClassTemplateNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;
            VisitBlock(node);
            currentSymbolPath = currentScope;
        }

        public void VisitFunc(FuncNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;

            string path = currentSymbolPath;
            SymbolData data = SymbolTableManager.GetSymbol(path, currentSymbolPath);
            if (data == null)
            {
                return;
            }

            SymbolData typeData = SymbolTableManager.GetSymbol(data.type, currentSymbolPath);
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

            VisitBlock(node);
            currentSymbolPath = currentScope;
        }

        public void VisitVarDeclare(VarDeclareNode node)
        {
            // Check var type
            string path = currentSymbolPath + "." + node.Name;
            SymbolData data = SymbolTableManager.GetSymbol(path);
            if (data == null)
                return;

            SymbolData typeData = SymbolTableManager.GetSymbol(data.type);
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
        }

        public void VisitFuncCall(FuncCallNode node)
        {
            if (!node.Name.Contains('.'))
            {
                // Accessible from this scope
                if (SymbolTableManager.ContainsSymbol(node.Name, currentSymbolPath))
                {
                    SymbolData data = SymbolTableManager.GetSymbol(node.Name, currentSymbolPath);
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

                    // Fix argument types
                    int i = 0;
                    foreach (var kvp in remoteArgs)
                    {
                        string remoteType = SymbolTableManager.GetSymbol(kvp.Key.Name, currentSymbolPath).type;

                        symbolTable.UpdateSymbol(kvp.Key.Name, new()
                        {
                            fullName = data.fullName,
                            name = data.name,
                            nationality = data.nationality,
                            type = remoteType,
                            data = data.data
                        }, node, analyser);

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

                        string localType = SymbolTableManager.GetSymbol(localArgs[i].StringType, currentSymbolPath).fullName;

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
                    if (!SymbolTableManager.ContainsSymbol(path, currentSymbolPath))
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

        public void VisitNameReference(NameReferenceNode node)
        {
            // TODO
        }

        public void VisitString(StringNode node)
        {
            return; // No need for pass 2
        }

        public void VisitVarAssign(VarAssignNode node)
        {
            // TODO
        }
    }
}
