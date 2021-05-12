using Marlin.Parsing;

namespace Marlin.CodeGen
{
    public abstract unsafe class Target : IVisitor
    {
        public static long TotalCodeGenTime { get; protected set; } = 0;

        public abstract void BeginTranslation(Node ast);

        public virtual void Visit(Node node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitBinaryOperator(BinaryOperatorNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitBlock(Node node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitBoolean(BooleanNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitClassTemplate(ClassTemplateNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitDouble(NumberDoubleNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitFunc(FuncNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitFuncCall(FuncCallNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitInteger(NumberIntegerNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitNameReference(NameReferenceNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitReturn(ReturnNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitString(StringNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitVarAssign(VarAssignNode node)
        {
            throw new System.NotImplementedException();
        }

        public virtual void VisitVarDeclare(VarDeclareNode node)
        {
            throw new System.NotImplementedException();
        }
    }
}
