using Marlin.Parsing;
using System;
using static Marlin.SemanticAnalysis.SymbolData;

namespace Marlin.SemanticAnalysis
{
    public class PassOneVisitor : IVisitor
    {
        private readonly SymbolTableManager symbolTable;
        private readonly MarlinSemanticAnalyser analyser;

        private string currentSymbolPath = "__global__";

        public PassOneVisitor(SymbolTableManager symbolTable, MarlinSemanticAnalyser analyser)
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
            Visit(node.Left);
            Visit(node.Right);
        }

        public void VisitClassTemplate(ClassTemplateNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;

            symbolTable.AddSymbol(currentSymbolPath, new()
            {
                fullName = currentSymbolPath,
                name = node.Name,
                nationality = SymbolNationality.CLASS,
                type = node.Name
            }, node, analyser);
            
            VisitBlock(node);
            currentSymbolPath = currentScope;
        }

        public void VisitFunc(FuncNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;
            
            symbolTable.AddSymbol(currentSymbolPath, new()
            {
                fullName = currentSymbolPath,
                name = node.Name,
                nationality = SymbolNationality.FUNC,
                type = node.FuncType
            }, node, analyser);

            // Args
            foreach (var arg in node.Args)
            {
                symbolTable.AddSymbol(currentSymbolPath + "." + arg.Value.Name, new()
                {
                    fullName = currentSymbolPath + "." + arg.Value.Name,
                    name = arg.Value.Name,
                    nationality = SymbolNationality.VARIABLE,
                    type = arg.Key.Name
                }, node, analyser);
            }

            // Function members
            VisitBlock(node);
            currentSymbolPath = currentScope;
        }

        public void VisitVarDeclare(VarDeclareNode node)
        {
            symbolTable.AddSymbol(currentSymbolPath + "." + node.Name, new()
            {
                fullName = currentSymbolPath + "." + node.Name,
                name = node.Name,
                nationality = SymbolNationality.VARIABLE,
                type = node.VarType
            }, node, analyser);
        }

        #region Empty (no pass 1 importance)
        public void VisitFuncCall(FuncCallNode node)
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

        #endregion
    }
}
