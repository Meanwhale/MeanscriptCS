namespace Meanscript {

public class NodeIterator {
internal MNode node;

public NodeIterator (MNode _node)
{
	node = _node;
}

public NodeIterator (NodeIterator it) { node = it.node; }

public NodeIterator  copy ()
{
	return new NodeIterator(node);
}
public int  type ()
{
	return node.type;
}
public MSText  data ()
{
	return node.data;
}
public MNode  getChild()
{
	return node.child;
}
public MNode  getNext()
{
	return node.next;
}
public MNode  getParent()
{
	return node.parent;
}
public int  numChildren ()
{
	return node.numChildren;
}
public int  line ()
{
	return node.lineNumber;
}
public bool hasNext()
{
	return node.next != null;
}
public bool hasChild()
{
	return node.child != null;
}
public bool hasParent()
{
	return node.parent != null;
}
public int nextType() 
{
	MS.assertion(hasNext(),MC.EC_INTERNAL, "nextType: no next");
	return node.next.type;
}
public void toNext() 
{
	MS.assertion(hasNext(),MC.EC_INTERNAL, "toNext: no next");
	node = node.next;
}
public bool toNextOrFalse()
{
	if (!hasNext()) return false;
	node = node.next;
	return true;
}
public void toChild() 
{
	MS.assertion(hasChild(),MC.EC_INTERNAL, "toChild: no child");
	node = node.child;
}
public void toParent() 
{
	MS.assertion(hasParent(),MC.EC_INTERNAL, "toParent: no parent");
	node = node.parent;
}
public void printTree(bool deep) 
{
	node.printTree(deep);
}
}
}
