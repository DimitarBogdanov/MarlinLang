/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     Node.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

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
        TYPE_REFERENCE,
        VARIABLE_ASSIGNMENT,
        VARIABLE_REFERENCE,
        NUMBER_INT,
        NUMBER_DBL,
    }

    public class Node
    {
        public string Id { get; set; } = "";
        public Node Parent { get; set; } = null;
        public List<Node> Children { get; set; } = new();
        public NodeType Type { get; set; } = NodeType.BLOCK;

        public Node(string id = "")
        {
            if (id == "")
                id = Guid.NewGuid().ToString();

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
    }

    public class BinaryOperatorNode : Node
    {
        public string Value { get; private set; }
        public Node Left { get; private set; }
        public Node Right { get; private set; }

        public BinaryOperatorNode(string value, Node left, Node right)
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
    }

    public class ClassTemplateNode : Node
    {
        public string Name { get; private set; }

        public ClassTemplateNode(string name)
        {
            Name = name;

            Type = NodeType.CLASS_TEMPLATE;
        }

        public override string ToString()
        {
            return "ClassTemplate<" + Name + ">";
        }
    }

    public class FuncNode : Node
    {
        public string Name { get; private set; }
        public List<KeyValuePair<VarNode, VarNode>> Args { get; private set; }

        public FuncNode(string name, List<KeyValuePair<VarNode, VarNode>> args)
        {
            Name = name;
            Args = args;

            Type = NodeType.FUNCTION;
        }

        public override string ToString()
        {
            return "Func<" + Name + ">\n" + Args.Count + " arg(s)";
        }
    }

    public class FuncCallNode : Node
    {
        public string Name { get; private set; }

        public FuncCallNode(string name, List<Node> args)
        {
            Name = name;
            Children = args;

            Type = NodeType.FUNCTION_CALL;
        }

        public override string ToString()
        {
            return "Call<" + Name + ">\n" + Children.Count + " arg(s)";
        }
    }

    public class VarNode : Node
    {
        public string Name { get; private set; }

        public VarNode(string name)
        {
            Name = name;

            Type = NodeType.VARIABLE_REFERENCE;
        }

        public override string ToString()
        {
            return "VarRef<" + Name + ">";
        }
    }

    public class VarAssignNode : Node
    {
        public string Name { get; private set; }
        public Node Value { get; private set; }

        public VarAssignNode(string name, Node value)
        {
            Name = name;
            Value = value;
            Children.Add(value);

            Type = NodeType.VARIABLE_ASSIGNMENT;
        }

        public override string ToString()
        {
            return "VarAssign<" + Name + ">";
        }
    }

    #region Numbers
    public class NumberIntegerNode : Node
    {
        public int Value { get; private set; }

        public NumberIntegerNode(int value)
        {
            Value = value;

            Type = NodeType.NUMBER_INT;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class NumberDoubleNode : Node
    {
        public double Value { get; private set; }

        public NumberDoubleNode(double value)
        {
            Value = value;

            Type = NodeType.NUMBER_DBL;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
    #endregion Numbers
}
