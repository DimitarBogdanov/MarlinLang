using Marlin.Parsing;
using System;
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

            SymbolData data = SymbolTableManager.GetSymbol(currentSymbolPath);

            if (!SymbolTableManager.ContainsSymbol(data.type, currentSymbolPath))
            {
                analyser.AddWarning(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.UNKNOWN_SYMBOL,
                    message: "cannot find symbol '" + data.type + "'",
                    file: file,
                    rootCause: node.Token
                ));
            }
            else
            {
                // Update type (because it may need to be unboxed)
                data.type = SymbolTableManager.GetSymbol(data.type).type;
                symbolTable.UpdateSymbol(currentSymbolPath, data, node, analyser);
            }

            VisitBlock(node);
            currentSymbolPath = currentScope;
        }

        public void VisitVarDeclare(VarDeclareNode node)
        {
            string path = currentSymbolPath + "." + node.Name;
            SymbolData data = SymbolTableManager.GetSymbol(path);

            if (!SymbolTableManager.ContainsSymbol(data.type, path))
            {
                analyser.AddWarning(new(
                    level: Level.ERROR,
                    source: Source.SEMANTIC_ANALYSIS,
                    code: ErrorCode.UNKNOWN_SYMBOL,
                    message: "cannot find symbol '" + data.type + "'",
                    file: file,
                    rootCause: node.Token
                ));
            }
            else
            {
                // Update type (because it may need to be unboxed)
                data.type = SymbolTableManager.GetSymbol(data.type).type;
                symbolTable.UpdateSymbol(path, data, node, analyser);
                Console.WriteLine("poop " + path);
            }
        }

        public void VisitFuncCall(FuncCallNode node)
        {
            // TODO
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
            return; // No need for pass 2
        }
    }
}
