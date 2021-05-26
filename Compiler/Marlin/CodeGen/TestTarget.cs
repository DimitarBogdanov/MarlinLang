using Marlin.Parsing;
using Marlin.SemanticAnalysis;
using System;
using System.Collections.Generic;
using System.IO;

namespace Marlin.CodeGen
{
    // This is a target used to test the compiler.
    public class TestTarget : Target
    {
        private class Instruction
        {
            public Instruction(string type, string value = "", string comment = "")
            {
                Type = type;
                Value = value;
                Comment = comment;
                Indentation = indent;
            }

            public string Type { get; } = "";
            public string Value { get; } = "";
            public string Comment { get; } = "";
            public int Indentation { get; } = 0;
        }

        private readonly List<Instruction> instructions = new();
        private static int indent = 0;

        private string GetNameFromST(string exactPath)
        {
            return SymbolTable.GetSymbol(exactPath).name;
        }

        public override void BeginTranslation(Node ast)
        {
            instructions.Add(new("moduledef", Program.MODULE_NAME));

            VisitBlock(ast);
        }

        public override void Dump(string path)
        {
            path += "dump.simpleil";
            using StreamWriter writer = File.CreateText(path);
            writer.WriteLine("; This is a custom IL used solely for testing the compiler.");
            foreach (Instruction instr in instructions)
            {

                string indent = ((instr.Indentation != 0) ? " ".PadRight(instr.Indentation * 3) : "");
                string type = instr.Type;
                string value = instr.Value.Replace("\n", "\\n").Replace("\r", "");
                string comment = (instr.Comment != "") ? " ; " + instr.Comment + "\n": "";
                writer.WriteLine(indent + type + (value == "" ? "" : " ".PadRight(15 - type.Length) + value) + comment);
            }
        }

        public override void Visit(Node node)
        {
            node.Accept(this);
        }

        public override void VisitBlock(Node node)
        {
            foreach (Node n in node.Children)
                Visit(n);
        }

        public override void VisitClassTemplate(ClassTemplateNode node)
        {
            string name = GetNameFromST(node.Name);
            instructions.Add(new("classdef", name));
            instructions.Add(new("{"));
            indent++;
            VisitBlock(node);
            indent--;
            instructions.Add(new("}", "", "End of class " + name));
        }

        public override void VisitFunc(FuncNode node)
        {
            string name = GetNameFromST(node.Name);
            instructions.Add(new("funcdef", name));
            instructions.Add(new(".functype", node.FuncType));
            foreach (string attribute in node.Attributes)
                instructions.Add(new(".attribute", attribute));
            instructions.Add(new("{"));
            indent++;
            VisitBlock(node);
            indent--;
            instructions.Add(new("}", "", "End of function " + name));
        }

        public override void VisitReturn(ReturnNode node)
        {
            if (node.Value != null)
            {
                Visit(node.Value);
                instructions.Add(new("ret"));
            }
            else
            {
                instructions.Add(new("retvoid"));
            }
        }

        public override void VisitBinaryOperator(BinaryOperatorNode node)
        {
            Visit(node.Right);
            Visit(node.Left);

            instructions.Add(new("binopaction", node.Token.value));
        }

        public override void VisitFuncCall(FuncCallNode node)
        {
            instructions.Add(new("call", node.Name));
        }

        public override void VisitInteger(NumberIntegerNode node)
        {
            instructions.Add(new("loadint", node.Value.ToString()));
        }

        public override void VisitString(StringNode node)
        {
            instructions.Add(new("loadstring", "\"" + node.Value + "\""));
        }
    }
}
