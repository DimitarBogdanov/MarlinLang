/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     PassOneVisitor.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Parsing;
using System;
using System.Collections.Generic;
using static Marlin.SemanticAnalysis.SymbolData;

namespace Marlin.SemanticAnalysis
{
    public class PassOneVisitor : IVisitor
    {
        private readonly SymbolTable symbolTable;
        private readonly MarlinSemanticAnalyser analyser;

        private string currentSymbolPath = "__global__";
        private static byte constructorAmount = 0;

        public PassOneVisitor(SymbolTable symbolTable, MarlinSemanticAnalyser analyser)
        {
            this.symbolTable = symbolTable;
            this.analyser = analyser;
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
                case NodeType.NEW_CLASS_INSTANCE:
                    VisitNewClassInst((NewClassInstNode)node);
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
                case NodeType.CLASS_CONSTRUCTOR:
                    VisitConstructor((ConstructorNode)node);
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
            Visit(node.Left);
            Visit(node.Right);

            return;
        }

        public void VisitClassTemplate(ClassTemplateNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;
            byte oldConstructorAmount = constructorAmount;
            constructorAmount = 0;
            
            symbolTable.AddSymbol(currentSymbolPath, new()
            {
                fullName = currentSymbolPath,
                name = node.Name,
                nationality = SymbolNationality.CLASS,
                type = currentSymbolPath,
                data = new()
            }, node, analyser);

            VisitBlock(node);
            currentSymbolPath = currentScope;

            constructorAmount = oldConstructorAmount;

            return;
        }

        public void VisitFunc(FuncNode node) 
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;

            Dictionary<NameReferenceNode, NameReferenceNode> args = new();

            // Args
            foreach (var arg in node.Args)
            {
                args.Add(arg.Key, arg.Value);
                symbolTable.AddSymbol(currentSymbolPath + "." + arg.Value.Name, new()
                {
                    fullName = currentSymbolPath + "." + arg.Value.Name,
                    name = arg.Value.Name,
                    nationality = SymbolNationality.VARIABLE,
                    type = arg.Key.Name,
                    data = new()
                }, node, analyser);
                arg.Key.Name = currentSymbolPath + "." + arg.Value.Name;
            }

            symbolTable.AddSymbol(currentSymbolPath, new()
            {
                fullName = currentSymbolPath,
                name = node.Name,
                nationality = SymbolNationality.FUNC,
                type = node.FuncType,
                data = new Dictionary<string, object>()
                {
                    ["args"] = args
                }
            }, node, analyser);

            // Function members
            VisitBlock(node);
            currentSymbolPath = currentScope;

            return;
        }

        public void VisitConstructor(ConstructorNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += ".constructor" + constructorAmount;

            Dictionary<NameReferenceNode, NameReferenceNode> args = new();

            // Args
            if (!node.IsStatic)
            {
                foreach (var arg in node.Args)
                {
                    args.Add(arg.Key, arg.Value);
                    symbolTable.AddSymbol(currentSymbolPath + "." + arg.Value.Name, new()
                    {
                        fullName = currentSymbolPath + "." + arg.Value.Name,
                        name = arg.Value.Name,
                        nationality = SymbolNationality.VARIABLE,
                        type = arg.Key.Name,
                        data = new()
                    }, node, analyser);
                    arg.Key.Name = currentSymbolPath + "." + arg.Value.Name;
                }
            }

            symbolTable.AddSymbol(currentSymbolPath, new()
            {
                fullName = currentSymbolPath,
                name = "constructor",
                nationality = SymbolNationality.FUNC,
                type = node.StringType,
                data = new Dictionary<string, object>()
                {
                    ["args"] = args
                }
            }, node, analyser);

            // Function members
            VisitBlock(node);
            currentSymbolPath = currentScope;

            constructorAmount++;

            return;
        }

        public void VisitVarDeclare(VarDeclareNode node)
        {
            symbolTable.AddSymbol(currentSymbolPath + "." + node.Name, new()
            {
                fullName = currentSymbolPath + "." + node.Name,
                name = node.Name,
                nationality = SymbolNationality.VARIABLE,
                type = node.VarType,
                data = new()
            }, node, analyser);

            return;
        }

        #region Empty (no pass 1 importance)
        public void VisitFuncCall(FuncCallNode node)
        {
            return; // No need for pass 1
        }

        public void VisitNewClassInst(NewClassInstNode node)
        {
            return; // No need for pass 1
        }

        public void VisitBoolean(BooleanNode node)
        {
            return; // No need for pass 1
        }

        public void VisitDouble(NumberDoubleNode node)
        {
            return; // No need for pass 1
        }

        public void VisitInteger(NumberIntegerNode node)
        {
            return; // No need for pass 1
        }

        public void VisitNameReference(NameReferenceNode node)
        {
            return; // No need for pass 1
        }

        public void VisitString(StringNode node)
        {
            return; // No need for pass 1
        }

        public void VisitVarAssign(VarAssignNode node)
        {
            return; // No need for pass 1
        }

        public void VisitReturn(ReturnNode node)
        {
            return; // No need for pass 1
        }

        #endregion
    }
}
