using System;
using System.Collections.Generic;

namespace Marlin.Parsing
{
    public enum NodeType
    {
        ROOT,
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
        public NodeType Type { get; set; } = NodeType.ROOT;

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

        public BinaryOperatorNode(string value)
        {
            Value = value;

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
