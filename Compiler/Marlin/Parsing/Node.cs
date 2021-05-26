﻿/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     Node.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Lexing;
using System;
using System.Collections.Generic;

namespace Marlin.Parsing
{
    public enum NodeType
    {
        BLOCK,
        BINARY_OPERATOR,
        FUNCTION,
        FUNCTION_CALL,
        CLASS_TEMPLATE,
        CLASS_CONSTRUCTOR,
        NEW_CLASS_INSTANCE,
        VARIABLE_ASSIGNMENT,
        VARIABLE_DECLARATION,
        RETURN_STATEMENT,
        NAME_REFERENCE,
        NUMBER_INT,
        NUMBER_DBL,
        STRING,
        BOOLEAN
    }

    public class Node
    {
        public string Id { get; set; } = "";
        public string StringType { get; set; } = "?";
        public Node Parent { get; set; } = null;
        public List<Node> Children { get; set; } = new();
        public NodeType Type { get; set; } = NodeType.BLOCK;
        public Token Token { get; set; } = null;

        public Node(Token token, string id = "")
        {
            if (id == "")
                id = Guid.NewGuid().ToString();

            Token = token;
            Id = id;
        }

        public void AddChild(Node child)
        {
            if (child != null)
            {
                child.Parent = this;
                Children.Add(child);
            }
        }

        public override string ToString()
        {
            return "";
        }

        public virtual void Accept(IVisitor visitor)
        {
            return;
        }
    }

    public class BinaryOperatorNode : Node
    {
        public string Value { get; private set; }
        public new string StringType
        {
            get
            {
                return Left.StringType;
            }
        }
        public Node Left { get; private set; }
        public Node Right { get; private set; }

        public BinaryOperatorNode(string value, Node left, Node right, Token token) : base(token)
        {
            Value = value;
            Left = left;
            Right = right;

            Children.Add(left);
            Children.Add(right);

            Type = NodeType.BINARY_OPERATOR;
        }

        public override string ToString()
        {
            return Value;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitBinaryOperator(this);
        }
    }

    public class ClassTemplateNode : Node
    {
        public string Name { get; set; }
        public string[] Attributes { get; }

        public ClassTemplateNode(string name, string[] attributes, Token token) : base(token)
        {
            Name = name;

            StringType = name;
            Type = NodeType.CLASS_TEMPLATE;
            Attributes = attributes;
        }

        public override string ToString()
        {
            return "ClassTemplate<" + Name + ">";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitClassTemplate(this);
        }
    }

    public class FuncNode : Node
    {
        public string Name { get; set; }
        public string FuncType { get; set; }
        public string[] Attributes { get; }
        public List<KeyValuePair<NameReferenceNode, NameReferenceNode>> Args { get; private set; }

        public FuncNode(string name, string type, string[] attributes, List<KeyValuePair<NameReferenceNode, NameReferenceNode>> args, Token token) : base(token)
        {
            Name = name;
            FuncType = type;
            Args = args;
            Attributes = attributes;

            StringType = "func";
            Type = NodeType.FUNCTION;
        }

        public override string ToString()
        {
            return "Func<" + Name + ">\n" + Args.Count + " arg(s)";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitFunc(this);
        }
    }

    public class ConstructorNode : Node
    {
        public bool IsStatic { get; set; }
        public List<KeyValuePair<NameReferenceNode, NameReferenceNode>> Args { get; private set; }

        public ConstructorNode(bool isStatic, string className, List<KeyValuePair<NameReferenceNode, NameReferenceNode>> args, Token token) : base(token)
        {
            IsStatic = isStatic;
            Args = args;

            StringType = className;
            Type = NodeType.CLASS_CONSTRUCTOR;
        }

        public override string ToString()
        {
            return "CTOR<>\n" + Args.Count + " arg(s)";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitConstructor(this);
        }
    }

    public class FuncCallNode : Node
    {
        public string Name { get; set; }
        public List<Node> Args { get; private set; }

        public FuncCallNode(string name, List<Node> args, Token token) : base(token)
        {
            Name = name;
            Args = args;
            Children = args;

            StringType = "?";
            Type = NodeType.FUNCTION_CALL;
        }

        public override string ToString()
        {
            return "Call<" + Name + ">\n" + Children.Count + " arg(s)";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitFuncCall(this);
        }
    }

    public class NewClassInstNode : Node
    {
        public string Name { get; set; }
        public List<Node> Args { get; private set; }

        public NewClassInstNode(string name, List<Node> args, Token token) : base(token)
        {
            Name = name;
            Args = args;
            Children = args;

            StringType = name;
            Type = NodeType.NEW_CLASS_INSTANCE;
        }

        public override string ToString()
        {
            return "Inst<" + Name + ">\n" + Children.Count + " arg(s)";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitNewClassInst(this);
        }
    }

    public class NameReferenceNode : Node
    {
        public string Name { get; set; }

        public NameReferenceNode(string name, Token token) : base(token)
        {
            Name = name;

            StringType = name;
            Type = NodeType.NAME_REFERENCE;
        }

        public override string ToString()
        {
            return "VarRef<" + Name + ">";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitNameReference(this);
        }
    }

    public class VarDeclareNode : Node
    {
        public string VarType { get; private set; }
        public string Name { get; private set; }
        public Node Value { get; private set; }

        public VarDeclareNode(string type, string name, Node value, Token token) : base(token)
        {
            VarType = type;
            Name = name;
            Value = value;
            Children.Add(value);

            StringType = VarType;
            Type = NodeType.VARIABLE_DECLARATION;
        }

        public override string ToString()
        {
            return "VarAssign<" + Name + ">";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitVarDeclare(this);
        }
    }

    public class VarAssignNode : Node
    {
        public NameReferenceNode Name { get; private set; }
        public Node Value { get; private set; }

        public VarAssignNode(NameReferenceNode name, Node value, Token token) : base(token)
        {
            Name = name;
            Value = value;
            Children.Add(value);

            StringType = value.StringType;
            Type = NodeType.VARIABLE_ASSIGNMENT;
        }

        public override string ToString()
        {
            return "VarAssign<" + Name + ">";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitVarAssign(this);
        }
    }

    public class ReturnNode : Node
    {
        public Node Value { get; private set; }

        public ReturnNode(Node value, Token token) : base(token)
        {
            Value = value;
            Children.Add(value);

            StringType = (value != null) ? value.StringType : "void";
            Type = NodeType.RETURN_STATEMENT;
        }

        public override string ToString()
        {
            return "Return<" + StringType + ">";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitReturn(this);
        }
    }

    public class StringNode : Node
    {
        public string Value { get; private set; }

        public StringNode(string value, Token token) : base(token)
        {
            Value = value;

            StringType = "string";
            Type = NodeType.STRING;
        }

        public override string ToString()
        {
            return "\"" + Value.ToString() + "\"";
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitString(this);
        }
    }

    public class BooleanNode : Node
    {
        public bool Value { get; private set; }

        public BooleanNode(bool value, Token token) : base(token)
        {
            Value = value;

            StringType = "boolean";
            Type = NodeType.BOOLEAN;
        }

        public override string ToString()
        {
            return Value.ToString().ToLower();
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitBoolean(this);
        }
    }

    public class NumberIntegerNode : Node
    {
        public int Value { get; private set; }

        public NumberIntegerNode(int value, Token token) : base(token)
        {
            Value = value;

            StringType = "int";
            Type = NodeType.NUMBER_INT;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitInteger(this);
        }
    }

    public class NumberDoubleNode : Node
    {
        public double Value { get; private set; }

        public NumberDoubleNode(double value, Token token) : base(token)
        {
            Value = value;

            StringType = "double";
            Type = NodeType.NUMBER_DBL;
        }

        public override string ToString()
        {
            return Value.ToString().Replace(',', '.');
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitDouble(this);
        }
    }
}
