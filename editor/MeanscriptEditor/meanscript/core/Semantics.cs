namespace Meanscript {

public class Semantics : MC {
internal TokenTree tree;
public int typeIDCounter;
internal int maxContexts;
internal int numContexts;
internal System.Collections.Generic.Dictionary<MSText, int> types = new System.Collections.Generic.Dictionary<MSText, int>(MS.textComparer);
internal StructDef [] typeStructDefs;
internal Context [] contexts;
public Context globalContext;
internal Context currentContext;

public Semantics (TokenTree _tree) 
{
	tree = _tree;
	typeIDCounter = MAX_MS_TYPES;

	typeStructDefs = new StructDef[MAX_TYPES];
	for (int i=0; i<MAX_TYPES; i++)
	{
		typeStructDefs[i] = null;
	}
	
	maxContexts = MS.globalConfig.maxFunctions;
	contexts = new Context[maxContexts];
	
	contexts[0] = new Context(this, -1, 0, -1); // global context
	globalContext = contexts[0];
	currentContext = globalContext;
	for (int i=1; i<maxContexts; i++)
	{
		contexts[i] = null;
	}
	numContexts=1;
}
//;

public void addElementaryType (int typeID, int size) 
{
	string name = primitiveNames[typeID];
	MS.verbose("Add elementary type [" + typeID + "] " + name);
	types[ new MSText(name)] =  typeID;
	StructDef sd = new StructDef(this, -1, typeID, size);
	typeStructDefs[typeID] = sd;
}

// public StructDef addCharsType (int numChars) 
// {
	// // words:
	// //		0:		size in characters (start stringToIntsWithSize() from here)
	// //		1...n:	characters + zero at the end, e.g. "mean\0" --> 2 words = (numChars / 4) + 1
	
	// int arraySize = (numChars / 4) + 2;
	// MS.syntaxAssertion(arraySize > 0 && arraySize < MS.globalConfig.maxArraySize, null, "invalid array size");
	// int typeID = typeIDCounter++;
	// StructDef sd = new StructDef(this, -1, typeID, numChars, arraySize, OP_CHARS_DEF);
	// typeStructDefs[typeID] = sd;
	// return sd;
// }

public bool hasType(MSText name)
{
	return (types.ContainsKey( name));
}

public bool hasType(int id)
{
	return typeStructDefs[id] != null;
}

public StructDef  getType (int id) 
{
	StructDef userType = typeStructDefs[id];
	MS.assertion(userType != null,MC.EC_INTERNAL, "Data type error");
	return userType;
}

public StructDef  getType (MSText name) 
{
	int id = types[ name];
	StructDef userType = typeStructDefs[id];
	MS.assertion(userType != null,MC.EC_INTERNAL, "Data type error: " + name);
	return userType;
}

public StructDef  getType (int id, NodeIterator itPtr) 
{
	StructDef userType = typeStructDefs[id];
	MS.syntaxAssertion(userType != null, itPtr, "Data type error: #" + id);
	return userType;
}

public StructDef  getType (MSText name, NodeIterator itPtr) 
{
	int id = types[ name];
	StructDef userType = typeStructDefs[id];
	MS.syntaxAssertion(userType != null, itPtr, "Data type error: " + name);
	return userType;
}

public bool inGlobal ()
{
	return currentContext == contexts[0];
}

public int  getTextID (MSText text)
{
	return tree.getTextID(text);
}

public MSText  getText (int id)
{
	return tree.getTextByID(id);
}

public Context  findContext (MSText name)
{
	int textID = getTextID(name);
	if (textID < 0) return null;
	for (int i=1; i<maxContexts; i++)
	{
		if (contexts[i] == null) continue;
		if (contexts[i].variables.nameID == textID) return contexts[i];			
	}
	return null;
}

public bool  isNameValidAndAvailable (string name) 
{
	MSText n = new MSText (name);
	return isNameValidAndAvailable(n);
}

public bool  isNameValidAndAvailable (MSText name) 
{
	// check it has valid characters
	if (!Parser.isValidName(name)) {
		return false;
	}
	
	if(name.numBytes() >= MS.globalConfig.maxNameLength) {
		MS.errorOut.print("name is too long, max length: " + (MS.globalConfig.maxNameLength) + " name: " + name);
		return false;
	}
	// return true if not reserved, otherwise print error message and return false
	
	if(findContext(name) != null) {
		MS.errorOut.print("unexpected function name: " + name);
		return false;
	}
	if((types.ContainsKey( name))) {
		MS.errorOut.print("unexpected type name: " + name);
		return false;
	}
	
	int nameID = getTextID(name);
	if (nameID >= 0)
	{
		// name is saved: check if it's used in contexts
				
		if (globalContext.variables.getTagAddressByNameID(nameID) >= 0) {
			MS.errorOut.print("duplicate variable name: " + name);
			return false;
		}	
		if (currentContext != globalContext)
		{
			if(currentContext.variables.getTagAddressByNameID(nameID) >= 0) {
				MS.errorOut.print("duplicate variable name: " + name);
				return false;
			}
		}
	}
	
	for(int i=0; i<NUM_KEYWORDS; i++)
	{
		if (name.match(keywords[i])) {
			MS.errorOut.print("unexpected keyword: " + name);
			return false;
		}
	}
	return true;
}

public void analyze (TokenTree tree) 
{
	MS.verbose(HORIZONTAL_LINE);
	MS.verbose("SEMANTIC ANALYZE");
	MS.verbose(HORIZONTAL_LINE);

	currentContext = contexts[0];

	NodeIterator it = new NodeIterator(tree.root);
	
	analyzeNode(it.copy()); // start from the first expression
	
	it = null;
	
	MS.verbose(HORIZONTAL_LINE);
	MS.verbose("CONTEXTS");
	MS.verbose(HORIZONTAL_LINE);
	for (int i=0; i<maxContexts; i++)
	{
		if (contexts[i] != null)
		{
			MS.verbose("-------- context ID: " + i);
			contexts[i].variables.print(this);
		}
	}
	MS.verbose(HORIZONTAL_LINE);
	MS.verbose("END ANALYZING");
	MS.verbose(HORIZONTAL_LINE);

	print();
}

public void print ()
{
}

public void analyzeNode (NodeIterator it) 
{
	
	while (true)
	{
		if (it.type() == NT_EXPR)
		{
			if (!it.hasChild())
			{
				MS.verbose("<EMPTY EXPR>");
			}
			else
			{
				// make a new iterator for child
				it.toChild();
				analyzeExpr(it.copy());
				it.toParent();
			}
			
			if (!it.hasNext()) return;

			it.toNext();
		}
		else
		{
			throw new MException(MC.EC_INTERNAL,"expression expected");
		}
	}
}

public void analyzeExpr (NodeIterator it) 
{
	if (MS._verboseOn) it.printTree(false);

	if (it.type() == NT_NAME_TOKEN)
	{
		Context context = findContext(it.data());
		
		if (context != null)
		{
			MS.verbose("-------- function call!!!");
		}
		else if (it.data().match(keywords[KEYWORD_FUNC_ID]))
		{
			// get return type

			MS.syntaxAssertion(it.hasNext(), it, "function return type expected");
			it.toNext();
			MS.syntaxAssertion(hasType(it.data()), it, "unknown return type");
			int returnType = types[ it.data()];

			// function name

			MS.syntaxAssertion(it.hasNext(), it, "function name expected");
			it.toNext();
			MS.syntaxAssertion(it.type() == NT_NAME_TOKEN, it, "function name expected");
			MSText functionName = it.data();

			MS.syntaxAssertion(isNameValidAndAvailable(functionName), it, "variable name error");
			
			MS.verbose("Create a new function: " + functionName);
			
			// create new context
			int functionNameID = tree.addText(functionName);
			Context funcContext = new Context(this, functionNameID, numContexts, returnType);
			contexts[numContexts++] = funcContext;

			// argument definition
			
			MS.syntaxAssertion(it.hasNext(), it, "argument definition expected");
			it.toNext();
			createStructDef(funcContext.variables, it.copy());
			funcContext.variables.argsSize = funcContext.variables.structSize;
			funcContext.numArgs = funcContext.variables.numMembers;
			
			// parse function body
			
			MS.syntaxAssertion(it.hasNext(), it, "function body expected");
			it.toNext();
			MS.syntaxAssertion(it.type() == NT_CODE_BLOCK, it, "code block expected");
			
			// save node to Context for Generator
			funcContext.codeNode = it.node;
			
			it.toChild();
			MS.verbose("-------- ANALYZE FUNCTION");
			currentContext = funcContext;
			analyzeNode(it.copy());
			currentContext = contexts[0];
			MS.verbose("-------- END ANALYZE FUNCTION");
			it.toParent();

			MS.syntaxAssertion(!it.hasNext(), it, "unexpected token after code block");
		}
		else if (it.data().match(keywords[KEYWORD_STRUCT_ID]))
		{
			// e.g. "struct Vec [int x, INT y, INT z]"

			MS.syntaxAssertion(it.hasNext(), it, "struct name expected");
			it.toNext();
			MSText structName = it.data();
			MS.syntaxAssertion(isNameValidAndAvailable(structName), it, "variable name error");
			MS.syntaxAssertion(it.hasNext(), it, "struct definition expected");
			it.toNext();
			MS.syntaxAssertion(!it.hasNext(), it, "unexpected token after struct definition");
			MS.verbose("Create a new struct: " + structName);
			addStructDef(structName, it.copy());
		}
		else if (hasType(it.data()))
		{
			// expr. starts with a type name, eg. "int foo" OR "person [5] players"
			
			int type = types[ it.data()];
			MS.assertion(type == MS_TYPE_INT || type == MS_TYPE_INT64 || type == MS_TYPE_FLOAT || type == MS_TYPE_FLOAT64 || type == MS_TYPE_BOOL || type == MS_TYPE_TEXT || type == MS_GEN_TYPE_CHARS || type >= MAX_MS_TYPES,MC.EC_INTERNAL, "semantics: unknown type: " + type);

			it.toNext();
			
			if (type == MS_GEN_TYPE_CHARS)
			{
				// get number of chars, eg. "chars [12] name"
				MS.syntaxAssertion(it.type() == NT_SQUARE_BRACKETS, it, "chars size expected");
				it.toChild();
				MS.syntaxAssertion(!it.hasNext(), it, "only the chars size expected");
				it.toChild();
				MS.syntaxAssertion(!it.hasNext(), it, "only the chars size expected");
				MS.syntaxAssertion(it.type() == NT_NUMBER_TOKEN, it, "chars size (number) expected");
				
				// parse size and calculate array size
				
				int charCount = MS.parseInt(it.data().getString());
				
				it.toParent();
				it.toParent();
				
				it.toNext();
				MS.syntaxAssertion(it.type() == NT_NAME_TOKEN, it, "name expected");
				MS.syntaxAssertion(isNameValidAndAvailable(it.data()), it, "variable name error");
				
				currentContext.variables.addChars(tree.addText(it.data()), charCount);
			}
			else if (it.type() == NT_SQUARE_BRACKETS)
			{
				// eg. "person [5] players"
				
				MS.syntaxAssertion(inGlobal(), it, "no arrays in functions");
				
				// array size
				it.toChild();
				MS.syntaxAssertion(!it.hasNext(), it, "only the array size expected");
				
				int arraySize = -1;
				
				if (!it.hasChild())
				{
					// array size is not specified, so argument count decide it
					// eg. if "int [] numbers: 1, 2, 3" then size is 3
				}
				else
				{
					it.toChild();
					MS.syntaxAssertion(!it.hasNext(), it, "array size expected");
					MS.syntaxAssertion(it.type() == NT_NUMBER_TOKEN, it, "array size (number) expected");
					arraySize = MS.parseInt(it.data().getString());
					MS.syntaxAssertion(arraySize > 0 && arraySize < MS.globalConfig.maxArraySize, it, "invalid array size");
					it.toParent();
				}
				it.toParent();
				
				// array name
				it.toNext();
				MS.syntaxAssertion(it.type() == NT_NAME_TOKEN, it, "name expected");
				MSText varName = it.data();
				MS.syntaxAssertion(isNameValidAndAvailable(varName), it, "variable name error");
				
				if (arraySize == -1)
				{
					it.toNext();
					MS.assertion(it.type() == NT_ASSIGNMENT,MC.EC_INTERNAL, "array assignment expected as the size is not defined");
					arraySize = it.numChildren();
					MS.syntaxAssertion(arraySize > 0 && arraySize < MS.globalConfig.maxArraySize, it, "invalid array size");
				}
				
				MS.verbose("New array: " + varName + ", size " + arraySize);

				currentContext.variables.addArray(tree.addText(varName), type, arraySize);
			}
			else
			{
				// variable name
				MS.syntaxAssertion(isNameValidAndAvailable(it.data()), it, "variable name error");
				MS.verbose("New variable: " + it.data() + " <" + getText(currentContext.variables.nameID) + ">");
				currentContext.variables.addMember(tree.addText(it.data()), type);
			}
		}
	}
	else
	{
		MS.assertion(false, EC_PARSE, "unexpected token");
	}
}

public void  addStructDef (MSText name, NodeIterator it) 
{
	// add user type
	
	int nameID = tree.addText(name);
	
	int typeID = typeIDCounter++;
	
	StructDef sd = new StructDef(this, nameID, typeID);
	createStructDef(sd, it.copy());

	sd.print(this);

	addStructDef(name, typeID, sd);
}

public void  addStructDef (MSText name, int id, StructDef sd) 
{
	MS.assertion(!(types.ContainsKey(name)) && typeStructDefs[sd.typeID] == null,MC.EC_INTERNAL, "addStructDef: type ID reserved");
	types[ new MSText(name)] =  (int)(id & VALUE_TYPE_MASK);
	typeStructDefs[sd.typeID] = sd;
}

public void createStructDef (StructDef sd, NodeIterator it) 
{	
	MS.syntaxAssertion(it.type() == NT_SQUARE_BRACKETS, it, "struct definition expected");

	it.toChild();
	MS.syntaxAssertion(it.type() == NT_EXPR, it, "exression expected");

	do
	{
		if (!it.hasChild()) continue; // skip an empty expression
		it.toChild();
		MS.syntaxAssertion((types.ContainsKey((it.data()))),it,  "createStructDef: unknown type: " + it.data());
		int type = types[ (it.data())];
		it.toNext();

		if (type == MS_GEN_TYPE_CHARS)
		{
			// get number of chars, eg. "chars [12] name"
			MS.syntaxAssertion(it.type() == NT_SQUARE_BRACKETS, it, "chars size expected");
			it.toChild();
			MS.syntaxAssertion(!it.hasNext(), it, "only the chars size expected");
			it.toChild();
			MS.syntaxAssertion(!it.hasNext(), it, "only the chars size expected");
			MS.syntaxAssertion(it.type() == NT_NUMBER_TOKEN, it, "chars size (number) expected");
			
			// parse size and calculate array size
			
			int charCount = MS.parseInt(it.data().getString());
			
			it.toParent();
			it.toParent();
			
			it.toNext();
			MS.syntaxAssertion(isNameValidAndAvailable(it.data()), it, "variable name error");
			
			sd.addChars(tree.addText(it.data()), charCount);
		}
		else if (it.type() == NT_SQUARE_BRACKETS)
		{
			// eg. "int [5] numbers"

			// NOTE: almost same as when defining variables...

			// array size
			it.toChild();
			MS.syntaxAssertion(!it.hasNext(), it, "array size expected");
			it.toChild();
			MS.syntaxAssertion(!it.hasNext(),it,  "array size expected");
			MS.syntaxAssertion(it.type() == NT_NUMBER_TOKEN, it, "array size (number) expected");
			int arraySize = MS.parseInt(it.data().getString());
			it.toParent();
			it.toParent();
			
			// array name
			it.toNext();
			MS.verbose("Member array: " + it.data() + ", size" + arraySize);
			
			sd.addArray(tree.addText(it.data()), type, arraySize);
		}
		else
		{
			MS.syntaxAssertion(it.type() == NT_NAME_TOKEN,it,  "member name expected");
			MS.syntaxAssertion(sd.getTagAddressByName((it.data())) < 0,it,  "duplicate name: " + it.data());
			MS.verbose("Add struct member: " + it.data());
			sd.addMember(tree.addText(it.data()), type);
		}
		MS.syntaxAssertion(!it.hasNext(), it, "break expected");
		
		it.toParent();
	}
	while(it.toNextOrFalse());
}

public void writeStructDefs (ByteCode bc)
{
	// write globals
	StructDef sd = contexts[0].variables;
	for (int a=0; a<sd.codeTop; a++)
	{
		bc.addWord(sd.code[a]);
	}
	
	// write user struct definitions to code
	for (int i=MAX_MS_TYPES; i<MAX_TYPES; i++)
	{
		if (typeStructDefs[i] == null) continue;
		sd = typeStructDefs[i];
		for (int a=0; a<sd.codeTop; a++)
		{
			bc.addWord(sd.code[a]);
		}
	}
}


}
}
