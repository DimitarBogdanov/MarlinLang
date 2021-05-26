using System;
using System.Collections.Generic;
using System.IO;
using LLVMSharp;
using Marlin.Parsing;

namespace Marlin.CodeGen
{
    public unsafe class LLVMSharpTarget : Target
    {
        private readonly LLVMModuleRef module;
        private readonly LLVMBuilderRef builder;
        private readonly Stack<LLVMValueRef> valueStack;
        private readonly Stack<FuncNode> evaluateFunctions;
        private readonly List<Dictionary<string, LLVMValueRef>> scopes;

        private string currentSymbolPath = "__global__";

        public LLVMSharpTarget(string name)
        {
            module = LLVM.ModuleCreateWithName(name);
            builder = LLVM.CreateBuilder();
            valueStack = new();
            evaluateFunctions = new();
            scopes = new();
        }

        private LLVMValueRef? GetVariableFromCurrentScope(string name)
        {
            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                if (scopes[i].TryGetValue(name, out LLVMValueRef value))
                {
                    return value;
                }
            }
            return null;
        }

        public override void BeginTranslation(Node ast)
        {
            long start = Utils.CurrentTimeMillis();

            // Define globals from the symbol table in here

            // Generate code
            foreach (Node node in ast.Children)
            {
                node.Accept(this);
            }

            TotalCodeGenTime += Utils.CurrentTimeMillis() - start;
        }

        public override void Dump(string path)
        {
            path += "dump.ll";
            File.Create(path).Close();
            LLVM.PrintModuleToFile(module, path, out string err);
            if (err != "")
                Console.WriteLine(err);
        }

        #region Visitor
        public override void Visit(Node node)
        {
            node.Accept(this);
        }

        public override void VisitBinaryOperator(BinaryOperatorNode node)
        {
            Visit(node.Right);
            Visit(node.Left);

            LLVMValueRef left = valueStack.Pop();
            LLVMValueRef right = valueStack.Pop();

            LLVMValueRef result = node.Value switch
            {
                "+" => LLVM.BuildFAdd(builder, left, right, "addtmp"),
                "-" => LLVM.BuildFSub(builder, left, right, "subtmp"),
                "*" => LLVM.BuildFMul(builder, left, right, "multmp"),
                "/" => LLVM.BuildFDiv(builder, left, right, "divtmp"),
                "<" => LLVM.BuildUIToFP(builder, LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealULT, left, right, "cmptmp"), LLVM.DoubleType(), "booltmp"),
                ">" => LLVM.BuildUIToFP(builder, LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealULT, right, left, "cmptmp"), LLVM.DoubleType(), "booltmp"),
                _ => throw new Exception("Unknown binary operator '" + node.Value + "' - this should have been caught in the parser."),
            };

            valueStack.Push(result);
        }

        public override void VisitBlock(Node node)
        {
            foreach (Node child in node.Children)
            {
                Visit(child);
            }
        }

        public override void VisitBoolean(BooleanNode node)
        {
            //return new LLVMBool(node.Value ? 1 : 0);
        }

        public override void VisitClassTemplate(ClassTemplateNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath = node.Name;

            VisitBlock(node);

            currentSymbolPath = currentScope;
            return;
        }

        public override void VisitNewClassInst(NewClassInstNode node)
        {
            return; // TODO
        }

        public override void VisitConstructor(ConstructorNode node)
        {
            return; // TODO
        }

        public override void VisitDouble(NumberDoubleNode node)
        {
            valueStack.Push(LLVM.ConstReal(LLVM.DoubleType(), node.Value));
        }

        public override void VisitFunc(FuncNode node)
        {
            string currentScope = currentSymbolPath;
            currentSymbolPath = node.Name;

            string funcType = node.FuncType;
            var argsCount = (uint)node.Args.Count;
            var arguments = new LLVMTypeRef[argsCount];

            scopes.Add(new());
            LLVMTypeRef typeRef;

            for (int i = 0; i < argsCount; i++)
            {
                // TODO: Marlin supports more types than just Int32s, lol
                arguments[i] = LLVM.Int32Type();
                var argData = node.Args[i];
                string type = argData.Value.Name;
                string name = argData.Value.Name;
                int lio = name.LastIndexOf('.');
                if (lio != -1)
                    name = name.Substring(lio + 1, name.Length - lio - 1);
                typeRef = new();

                if (type == "marlin.Int")
                {
                    typeRef = LLVM.Int32Type();
                }
                else if (type == "marlin.Long")
                {
                    typeRef = LLVM.Int64Type();
                }
                else if (type == "marlin.Double")
                {
                    typeRef = LLVM.DoubleType();
                }

                scopes[i].Add(name, LLVM.ConstNull(typeRef));
            }

            typeRef = new();

            if (funcType == "marlin.Int")
            {
                typeRef = LLVM.Int32Type();
            }
            else if (funcType == "marlin.Long")
            {
                typeRef = LLVM.Int64Type();
            }
            else if (funcType == "marlin.Double")
            {
                typeRef = LLVM.DoubleType();
            }

            var function = LLVM.AddFunction(module, node.Name, LLVM.FunctionType(LLVM.Int32Type(), arguments, new LLVMBool(0)));
            LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);

            LLVM.PositionBuilderAtEnd(builder, LLVM.AppendBasicBlock(function, "entry"));

            try
            {
                VisitBlock(node);
            }
            catch (Exception)
            {
                LLVM.DeleteFunction(function);
                throw;
            }

            // Finish off the function.
            LLVM.BuildRet(builder, valueStack.Pop());

            // Validate the generated code, checking for consistency.
            LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);

            valueStack.Push(function);

            currentSymbolPath = currentScope;
        }

        public override void VisitFuncCall(FuncCallNode node)
        {
            var calledFunc = LLVM.GetNamedFunction(module, node.Name);

            if (calledFunc.Pointer == IntPtr.Zero)
            {
                throw new Exception("Nonexistent func " + node.Name + " - this is a critical bug, this should have been caught by semanalysis");
            }

            if (LLVM.CountParams(calledFunc) != node.Args.Count)
            {
                throw new Exception("Incorrect # arguments passed to " + node.Name + " - this is a critical bug, this should have been caught by semanalysis");
            }

            var argumentCount = (uint)node.Args.Count;
            var argsV = new LLVMValueRef[argumentCount];
            for (int i = 0; i < argumentCount; ++i)
            {
                Visit(node.Args[i]);
                argsV[i] = valueStack.Pop();
            }

            valueStack.Push(LLVM.BuildCall(builder, calledFunc, argsV, "calltmp"));
        }

        public override void VisitInteger(NumberIntegerNode node)
        {
            valueStack.Push(LLVM.ConstInt(LLVM.Int32Type(), (ulong)node.Value, new LLVMBool(1)));
        }

        public override void VisitNameReference(NameReferenceNode node)
        {
            LLVMValueRef? value = GetVariableFromCurrentScope(node.Name);
            if (value != null)
            {
                valueStack.Push((LLVMValueRef)value);
            }
            else
            {
                throw new Exception("Unknown variable name '" + node.Name + "' - this should have been caught in semanalysis.");
            }
        }

        public override void VisitReturn(ReturnNode node)
        {
            // This will push the return value to the stack :D
            Visit(node.Value);
        }

        public override void VisitString(StringNode node)
        {
            valueStack.Push(LLVM.ConstString(node.Value, (uint)node.Value.Length, new LLVMBool(0)));
        }

        public override void VisitVarAssign(VarAssignNode node)
        {
            throw new NotImplementedException();
        }

        public override void VisitVarDeclare(VarDeclareNode node)
        {
            throw new NotImplementedException();
        }
        #endregion Visitor
    }
}
