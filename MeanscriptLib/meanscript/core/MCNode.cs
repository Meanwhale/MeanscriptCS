namespace Meanscript.Core
{

	public class MCNode
	{
		internal NodeType type;
		internal int numChildren;
		internal int lineNumber;
		internal int characterNumber;
		internal MSText data;
		internal long numeralValue;
		internal MCNode next = null;
		internal MCNode child = null;
		internal MCNode parent = null;

		public MCNode(int line, int ch, MCNode _parent, NodeType _type, MSText _data = null)
		{
			data = _data;
			lineNumber = line;
			characterNumber = ch;

			parent = _parent;
			type = _type;
			numChildren = 0;
		}
		public MCNode(int line, int ch, MCNode _parent, NodeType _type, long _numeralValue)
		{
			data = null;
			numeralValue = _numeralValue;
			lineNumber = line;
			characterNumber = ch;

			parent = _parent;
			type = _type;
			numChildren = 0;
		}

		public int Line()
		{
			return lineNumber;
		}

		public void PrintTree(bool deep)
		{
			PrintTree(this, 0, deep);
			if (!deep) MS.Print("");
		}

		public void PrintTree(MCNode _node, int depth, bool deep)
		{
			MS.Assertion(_node != null, MC.EC_INTERNAL, "<printTree: empty node>");

			MCNode node = _node;

			for (int i = 0; i < depth; i++) MS.Printn("  ");
			
			if (node.data == null) MS.Printn("[" + node.type + "]");
			else MS.Printn("[" + node.data + "]");

			// if (node.numChildren > 0) { MS.verbose(" + " + node.numChildren); }

			if (deep) MS.Print("");

			if (node.child != null && deep) PrintTree(node.child, depth + 1, deep);
			if (node.next != null) PrintTree(node.next, depth, deep);
		}
	}
}
