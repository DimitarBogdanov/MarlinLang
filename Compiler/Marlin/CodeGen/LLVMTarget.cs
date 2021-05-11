using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using LLVMSharp.Interop;
using Marlin.Parsing;

namespace Marlin.CodeGen
{
    public unsafe class LLVMTarget : IVisitor
    {
        private readonly LLVMModuleRef module;
        private readonly LLVMBuilderRef builder;
        private readonly Stack<LLVMValueRef> valueStack = new();

        private string currentSymbolPath = "__global__";

        public LLVMTarget(string name)
        {
            module = LLVM.ModuleCreateWithName(Utils.UnsafeStringToSByte(name));
            builder = LLVM.CreateBuilder();
        }

        public void BeginTranslation(Node ast)
        {
            // Define globals from the symbol table in here

            // Generate code
            foreach (Node node in ast.Children)
            {
                node.Accept(this);
            }
        }

        #region Visitor
        public void Visit(Node node)
        {
            throw new NotImplementedException();
        }

        public void VisitBinaryOperator(BinaryOperatorNode node)
        {
            throw new NotImplementedException();
        }

        public void VisitBlock(Node node)
        {
            throw new NotImplementedException();
        }

        public void VisitBoolean(BooleanNode node)
        {
            throw new NotImplementedException();
        }

        public void VisitClassTemplate(ClassTemplateNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;

            throw new NotImplementedException("Class templates in the LLVM target");

            currentSymbolPath = currentScope;
            return;
        }

        public void VisitDouble(NumberDoubleNode node)
        {
            valueStack.Push(LLVM.ConstReal(LLVM.DoubleType(), node.Value));
        }

        public void VisitFunc(FuncNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath += "." + node.Name;

            throw new NotImplementedException("Functions in the LLVM target");

            currentSymbolPath = currentScope;
        }

        public void VisitFuncCall(FuncCallNode node)
        {
            throw new NotImplementedException();
        }

        public void VisitInteger(NumberIntegerNode node)
        {
            valueStack.Push(LLVM.ConstReal(LLVM.Int32Type(), node.Value));
        }

        public void VisitNameReference(NameReferenceNode node)
        {
            throw new NotImplementedException();
        }

        public void VisitReturn(ReturnNode node)
        {
            throw new NotImplementedException();
        }

        public void VisitString(StringNode node)
        {
            valueStack.Push(LLVM.ConstString(Utils.UnsafeStringToSByte(node.Value), (uint)node.Value.Length, 0));
        }

        public void VisitVarAssign(VarAssignNode node)
        {
            throw new NotImplementedException();
        }

        public void VisitVarDeclare(VarDeclareNode node)
        {
            throw new NotImplementedException();
        }
        #endregion Visitor
    }
}
