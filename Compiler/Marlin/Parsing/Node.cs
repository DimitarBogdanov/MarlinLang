using System.Collections.Generic;

namespace Marlin.Parsing
{
    public enum NodeType
    {
        ROOT,
        BINARY_OPERATOR,
        FUNCTION,
        FUNCTION_CALL,
        CLASS_DEFINITION,
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
            Id = id;
        }

        public void AddChild(Node child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public override string ToString()
        {
            return "";
        }
    }

    public class BinaryOperatorNode : Node
    {
        public string Value { get; private set; }

        public BinaryOperatorNode(string id, string value) : base(id)
        {
            Value = value;

            Type = NodeType.BINARY_OPERATOR;
        }

        public override string ToString()
        {
            return Value;
        }
    }

    #region Numbers
    public class NumberIntegerNode : Node
    {
        public int Value { get; private set; }

        public NumberIntegerNode(string id, int value) : base(id)
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

        public NumberDoubleNode(string id, double value) : base(id)
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
