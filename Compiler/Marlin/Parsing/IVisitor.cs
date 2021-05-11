/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     IVisitor.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

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
        public void VisitReturn(ReturnNode node);
        public void VisitVarDeclare(VarDeclareNode node);
        public void VisitString(StringNode node);
        public void VisitBoolean(BooleanNode node);
        public void VisitInteger(NumberIntegerNode node);
        public void VisitDouble(NumberDoubleNode node);
    }
}
