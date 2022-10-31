namespace Meanscript {

public class MNode {
internal int type;
internal int numChildren;
internal int lineNumber;
internal int characterNumber;
internal MSText data;
internal long numeralValue;
internal MNode next = null;
internal MNode child = null;
internal MNode parent = null;

public MNode (int line, int ch, MNode _parent, int _type, MSText _data)
{
	data = _data;
	lineNumber = line;
	characterNumber = ch;

	parent = _parent;
	type = _type;
	numChildren = 0;
}
public MNode (int line, int ch, MNode _parent, int _type, long _numeralValue)
{
	data = null;
	numeralValue = _numeralValue;
	lineNumber = line;
	characterNumber = ch;

	parent = _parent;
	type = _type;
	numChildren = 0;
}

public int line ()
{
	return lineNumber;
}

public void printTree (bool deep) 
{
	printTree(this, 0, deep);
	if (!deep) MS.print("");
}

public void printTree (MNode _node, int depth, bool deep) 
{
	MS.assertion(_node != null,MC.EC_INTERNAL, "<printTree: empty node>");

	MNode node = _node;

	for (int i = 0; i < depth; i++) MS.printn("  ");

	MS.printn("[" + node.data + "]");

	// if (node.numChildren > 0) { MS.verbose(" + " + node.numChildren); }

	if (deep) MS.print("");

	if (node.child != null && deep) printTree(node.child, depth + 1, deep);
	if (node.next != null) printTree(node.next, depth, deep);
}
//

}
}
