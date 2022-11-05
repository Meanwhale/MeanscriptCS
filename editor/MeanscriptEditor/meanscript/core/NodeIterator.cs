namespace Meanscript
{

	public class NodeIterator
	{
		internal MNode node;

		public NodeIterator(MNode _node)
		{
			node = _node;
		}

		public NodeIterator(NodeIterator it) { node = it.node; }

		public NodeIterator Copy()
		{
			return new NodeIterator(node);
		}
		public NodeType Type()
		{
			return node.type;
		}
		public MSText Data()
		{
			return node.data;
		}
		public MNode GetChild()
		{
			return node.child;
		}
		public MNode GetNext()
		{
			return node.next;
		}
		public MNode GetParent()
		{
			return node.parent;
		}
		public int NumChildren()
		{
			return node.numChildren;
		}
		public int Line()
		{
			return node.lineNumber;
		}
		public bool HasNext()
		{
			return node.next != null;
		}
		public bool HasChild()
		{
			return node.child != null;
		}
		public bool HasParent()
		{
			return node.parent != null;
		}
		public NodeType NextType()
		{
			MS.Assertion(HasNext(), MC.EC_INTERNAL, "nextType: no next");
			return node.next.type;
		}
		public void ToNext()
		{
			MS.Assertion(HasNext(), MC.EC_INTERNAL, "toNext: no next");
			node = node.next;
		}
		public bool ToNextOrFalse()
		{
			if (!HasNext()) return false;
			node = node.next;
			return true;
		}
		public void ToChild()
		{
			MS.Assertion(HasChild(), MC.EC_INTERNAL, "toChild: no child");
			node = node.child;
		}
		public void ToParent()
		{
			MS.Assertion(HasParent(), MC.EC_INTERNAL, "toParent: no parent");
			node = node.parent;
		}
		public void PrintTree(bool deep)
		{
			node.PrintTree(deep);
		}
	}
}
