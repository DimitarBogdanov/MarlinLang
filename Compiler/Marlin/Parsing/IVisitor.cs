using Marlin.Parsing;

namespace Marlin.Parsing
{
    public interface IVisitor
    {
        public void Visit(Node node);
        public void VisitBlock(Node node);
        public void VisitBinaryOperator(BinaryOperatorNode node);
        public void VisitFunc(FuncNode node);
        public void VisitFuncCall(FuncCallNode node);
        public void VisitClassTemplate(ClassTemplateNode node);
        public void VisitNameReference(NameReferenceNode node);
        public void VisitVarAssign(VarAssignNode node);
        public void VisitVarDeclare(VarDeclareNode node);
        public void VisitString(StringNode node);
        public void VisitBoolean(BooleanNode node);
        public void VisitInteger(NumberIntegerNode node);
        public void VisitDouble(NumberDoubleNode node);
    }
}
