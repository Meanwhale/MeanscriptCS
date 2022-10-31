namespace Meanscript {

public class Generator : MC {
Context currentContext;
TokenTree tree;
Semantics sem;
Common common;
ByteCode bcPtr;
ByteCode bc;

public Generator (TokenTree _tree, Semantics _sem, Common _common)
{
	sem = _sem;
	common = _common;
	bc = new ByteCode(common);
	tree = _tree;
	currentContext = null;
}

//

public bool inGlobal ()
{
	return currentContext == sem.contexts[0];
}

public static ByteCode  generate (TokenTree _tree, Semantics _sem, Common _common) 
{
	Generator gen = new Generator(_tree, _sem, _common);
	gen.generate();
	return gen.bc;
}

public ByteCode generate () 
{
	
	MS.verbose("------------------------ GENERATE GLOBAL CODE");
	
	currentContext = sem.contexts[0];
	
	// start
	bc.addInstructionWithData(OP_START_INIT, 1, BYTECODE_EXECUTABLE, tree.textCount);
	
	// add texts (0 = empty)
	
	for (int i=1; i<tree.textCount; i++)
	{
		MSText t = sem.getText(i);
		MS.assertion(t != null,MC.EC_INTERNAL, "tree texts not containing text ID: " + i);
		bc.codeTop = addTextInstruction(t, OP_ADD_TEXT, bc.code, bc.codeTop);
	}
	
	// define structure types
	sem.writeStructDefs(bc);
	
	// introduce functions
	for (int i=0; i<sem.maxContexts; i++)
	{
		if (sem.contexts[i] != null)
		{
			sem.contexts[i].tagAddress = bc.codeTop;
			bc.addInstruction(OP_FUNCTION, 5, 0);
			bc.addWord(sem.contexts[i].functionID);
			bc.addWord(-1); // add start address later
			bc.addWord(-1); // add struct size later (temp. variables may be added)
			bc.addWord(sem.contexts[i].variables.argsSize);
			bc.addWord(-1); // add end address later...
			// ...where the 'go back' instruction is, or in the future
			// some local un-initialization or something.
			// 'return' takes you there.
		}
	}
	bc.addInstruction(OP_END_INIT, 0 , 0);
	
	currentContext = sem.contexts[0]; // = global
	currentContext.codeStartAddress = bc.codeTop;
	
	NodeIterator it = new NodeIterator(tree.root);
	
	generateCodeBlock(it.copy());
	
	it = null;
	
	currentContext.codeEndAddress = bc.codeTop;
	bc.addInstruction(OP_GO_BACK, 0 , 0); // end of global code
	
	for (int i=1; i<sem.maxContexts; i++)
	{
		if (sem.contexts[i] != null)
		{
			MS.verbose("------------------------ GENERATE FUNCTION CODE");
			currentContext = sem.contexts[i];
			NodeIterator iter = new NodeIterator(currentContext.codeNode);
			generateFunctionCode(iter.copy());
			iter = null;
		}
	}
	MS.verbose("------------------------ write code addresses");
	
	for (int i=0; i<sem.maxContexts; i++)
	{
		if (sem.contexts[i] != null)
		{
			bc.code[(sem.contexts[i].tagAddress) + 2] = sem.contexts[i].codeStartAddress;
			bc.code[(sem.contexts[i].tagAddress) + 3] = sem.contexts[i].variables.structSize;
			bc.code[(sem.contexts[i].tagAddress) + 5] = sem.contexts[i].codeEndAddress;
		}
	}
	MS.verbose("------------------------ END GENERATION");
	return bc;
}

public void generateFunctionCode (NodeIterator it) 
{
	it.toChild();
	currentContext.codeStartAddress = bc.codeTop;
	generateCodeBlock(it);
	currentContext.codeEndAddress = bc.codeTop;
	bc.addInstruction(OP_GO_BACK, 0, 0);
}

public void generateCodeBlock (NodeIterator it) 
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
				generateExpression(it.copy());
				it.toParent();
			}
			
			if (!it.hasNext()) return;

			it.toNext();
		}
		else
		{
			MS.syntaxAssertion(false, it, "expression expected");
		}
	}
}


public void generateExpression (NodeIterator it) 
{
	MS.verbose("------------ read expr ------------");
	if (MS._verboseOn) it.printTree(false);

	if (it.type() == NT_NAME_TOKEN)
	{
		Context context = sem.findContext(it.data());
		
		if (context != null)
		{
			generateFunctionCall(it, context);
		}
		else if ((common.callbackIDs.ContainsKey( it.data())))
		{
			generateCallbackCall(it);
		}
		else if(currentContext.variables.hasMember(it.data()))
		{
			generateAssignment(it);
		}
		else if (sem.hasType(it.data()))
		{
			MS.verbose("Initialize a variable");
			it.toNext();
			
			if (it.type() == NT_SQUARE_BRACKETS)
			{
				// eg. "int [] numbers" or "person [5] team"
				it.toNext();
			}

			MS.syntaxAssertion(currentContext.variables.hasMember(it.data()), it, "unknown variable: " + it.data());
			if (it.hasNext()) generateAssignment(it);
		}
		else if ((it.data()).match(keywords[KEYWORD_RETURN_ID]))
		{
			MS.verbose("Generate a return call");
			MS.syntaxAssertion(it.hasNext(),it,  "'return' is missing a value"); // TODO: return from a void context
			it.toNext();
			MS.syntaxAssertion(!it.hasNext(),it,  "'return' can take only one value");
			MS.syntaxAssertion(currentContext.returnType >= 0,it,  "can't return");
			
			// TODO: return value could be an array, a reference, etc.
			singleArgumentPush(currentContext.returnType, it, -1);
			
			bc.addInstruction(OP_POP_STACK_TO_REG, 1, currentContext.returnType);
			bc.addWord(sem.getType(currentContext.returnType, it).structSize);
			bc.addInstruction(OP_GO_END, 0 , 0);
		}
		else if ((it.data()).match(keywords[KEYWORD_STRUCT_ID]))
		{
			MS.verbose("Skip a struct definition");
		}
		else if ((it.data()).match(keywords[KEYWORD_FUNC_ID]))
		{
			MS.verbose("Skip a function definition for now");
		}
		else
		{
			MS.syntaxAssertion(false, it, "unknown word: " + it.data());
		}
	}
	else
	{
		MS.syntaxAssertion(false, it, "unexpected token");
	}
}

public void generateFunctionCall (NodeIterator it, Context funcContext) 
{
	MS.verbose("Generate a function call");
		
	bc.addInstruction(OP_SAVE_BASE, 0 , 0);
	
	if (funcContext.numArgs != 0)
	{
		MS.syntaxAssertion(it.hasNext(),it,  "function arguments expected");
		it.toNext();
		callArgumentPush(it.copy(), funcContext.variables, funcContext.numArgs);
	}
	bc.addInstructionWithData(OP_FUNCTION_CALL, 1, 0, funcContext.functionID);
	bc.addInstruction(OP_LOAD_BASE, 0 , 0);
}

public MCallback generateCallbackCall (NodeIterator it) 
{
	int callbackID = common.callbackIDs[ it.data()];
	MCallback callback = common.callbacks[callbackID];
	MS.verbose("Callback call, id " + callbackID);
	if (callback.argStruct.numMembers > 0)
	{
		it.toNext();
		callArgumentPush(it.copy(), callback.argStruct, callback.argStruct.numMembers);
	}
	bc.addInstructionWithData(OP_CALLBACK_CALL, 1, 0, callbackID);
	return callback;
}

public void generateAssignment(NodeIterator it) 
{
	// e.g. "int a:5" or "a:6"
	
	MS.verbose("Add value assinging instructions");
		
	// get assignment target 
	
	VarGen target = resolveMember(it);
	
	it.toNext();
	MS.assertion(it.type() == NT_ASSIGNMENT,MC.EC_INTERNAL, "assignment struct expected");

	if (target.isArray())
	{
		// assign array
		MS.syntaxAssertion(target.arraySize == it.numChildren(), it, "wrong number of arguments in array assignment");
		MS.syntaxAssertion(!target.isReference, it, "array reference can't be assigned");
		
		// assign children i.e. array items
		
		it.toChild();
		
		int arrayDataSize = arrayPush(it, target.type, target.arraySize);
		
		bc.addInstruction(inGlobal()?OP_POP_STACK_TO_GLOBAL:OP_POP_STACK_TO_LOCAL, 2, MS_TYPE_VOID);
		bc.addWord(arrayDataSize);
		bc.addWord(target.address);

		it.toParent();
		
		return;
	}

	StructDef typeSD = sem.getType(target.type, it);

	// get value for assignment target

	it.toChild();
	if (it.hasNext())
	{
		// list of arguments to assign
		argumentStructPush(it.copy(), typeSD, typeSD.numMembers, true);
	}
	else
	{
		NodeIterator cp = new NodeIterator(it);
		singleArgumentPush(target.type, cp, target.charCount); // last arg. > 0 if the type is chars
	}

	// WRITE values. This works like a callback call.
	// Actually here we could call overridden assignment callback for the type.

	// local or global?
	if (target.isReference)
	{
		bc.addInstruction(inGlobal()?OP_POP_STACK_TO_GLOBAL_REF:OP_POP_STACK_TO_LOCAL_REF, 2, MS_TYPE_VOID);
	}
	else
	{
		bc.addInstruction(inGlobal()?OP_POP_STACK_TO_GLOBAL:OP_POP_STACK_TO_LOCAL, 2, MS_TYPE_VOID);
	}
	
	if (target.type == MS_GEN_TYPE_CHARS)
	{
		bc.addWord(target.charCount/4 * 2);
	}
	else
	{
		MS.assertion(sem.getType(target.type, it).structSize > 0,MC.EC_INTERNAL, "...");
		bc.addWord(sem.getType(target.type, it).structSize);
	}
	
	bc.addWord(target.address);
}

public int arrayPush (NodeIterator it, int itemType, int arraySize) 
{
	StructDef itemSD = sem.getType(itemType);
	int itemSize = itemSD.structSize;

	for (int i=0; i<arraySize; i++)
	{
		it.toChild();
		NodeIterator cp = new NodeIterator(it);
		singleArgumentPush(itemType, cp, -1);
		it.toParent();
		if (it.hasNext()) it.toNext();
	}
	return arraySize * itemSize;
}

public void squareBracketArgumentPush (NodeIterator it, StructDef sd, int numArgs) 
{
	MS.verbose("Assign struct values in square brackets");
	
	int argIndex = 0;
	it.toChild();
	MS.assertion(it.type() == NT_EXPR,MC.EC_INTERNAL, "expression expected");
	MS.assertion(it.hasChild(),MC.EC_INTERNAL, "argument expected");

	do
	{
		it.toChild();
		MS.syntaxAssertion(argIndex < numArgs, it,  "wrong number of arguments, expected " + numArgs);
		int memberType = instrValueTypeID(sd.getMemberTagByIndex(argIndex));
		int numItems = -1;
		if (memberType == MS_GEN_TYPE_ARRAY)
		{
			numItems = sd.getMemberArrayItemCountOrNegative(argIndex);
		}
		if (memberType == MS_GEN_TYPE_CHARS)
		{
			numItems = sd.getMemberCharCount(argIndex);
		}
		singleArgumentPush(memberType, it, numItems);
		it.toParent();
		argIndex++;
	}
	while(it.toNextOrFalse());

	it.toParent();

	MS.syntaxAssertion(!(it.hasNext()) && argIndex == numArgs,it,  "wrong number of arguments");
}

public void callArgumentPush (NodeIterator it, StructDef sd, int numArgs) 
{
	if ((it.type() == NT_PARENTHESIS && !it.hasNext()))
	{
		// F2 (a1, a2)
		
		it.toChild();
		argumentStructPush(it, sd, numArgs, true);
	}
	else
	{
		// F1 a1
		// F2 a1 a2
		// F2 (F3 x) a2
		
		argumentStructPush(it, sd, numArgs, false);
	}
}

public void argumentStructPush (NodeIterator it, StructDef sd, int numArgs, bool commaSeparated) 
{
	MS.verbose("Assign struct argument");

	// HANDLE BOTH CASES:
	// 1)		func arg1 arg2
	// 2)		func (arg1, arg2)
	
	int argIndex = 0;
	do
	{		
		if (!commaSeparated)
		{
			MS.syntaxAssertion(!isFunctionOrCallback(it.data()),it, "function arguments must be in brackets or comma-separated");
		}
		else
		{
			it.toChild(); // comma-separated are expressions
		}
		
		MS.syntaxAssertion(sd.indexInRange(argIndex), it, "too many arguments");
		int memberTag = sd.getMemberTagByIndex(argIndex);
		int arrayItemCount = sd.getMemberArrayItemCountOrNegative(argIndex);
		singleArgumentPush(instrValueTypeID(memberTag), it, arrayItemCount);
		
		if (commaSeparated)
		{
			it.toParent();
		}
		
		argIndex++;
	}
	while(it.toNextOrFalse());

	MS.syntaxAssertion(!(it.hasNext()) && argIndex == numArgs, it, "wrong number of arguments");
}

public bool isFunctionOrCallback (MSText name)
{
	Context context = sem.findContext(name);
	if (context == null) return ((common.callbackIDs.ContainsKey( name)));
	return true;
}


public VarGen resolveMember (NodeIterator it) 
{
	// read a variable from a chain of nodes and return its address, type, etc.
	// as a VarGen. For example "x", "arr[foo].bar", "arr[f(a)].bar".
	
	bool isReference = false;
	int auxAddress = -1;
	int lastOffsetCodeIndex = -1;
	int arrayItemCount = -1;
	int charCount = -1;
	
	StructDef currentStruct = currentContext.variables;
	int memberType = (int)(currentStruct.getMemberTagByName(it.data()) & VALUE_TYPE_MASK);
	int size = currentStruct.getMemberSizeByName(it.data());
	int srcAddress = currentStruct.getMemberAddressByName(it.data());

	if (memberType == MS_GEN_TYPE_ARRAY) {
		arrayItemCount = currentStruct.getMemberArrayItemCount(it.data());
	} else if (memberType == MS_GEN_TYPE_CHARS) {
		charCount = currentStruct.getCharCount(it.data());
	} else {
		arrayItemCount = -1;
	}

	while (true)
	{
		MS.verbose("RESOLVER size:" + size + " mtype:" + memberType + " addr:" + srcAddress + " arrC:" + arrayItemCount + " charCount:" + charCount + " ref:" + isReference);
		
		if (it.hasNext() && it.nextType() == NT_DOT)
		{
			// e.g. "age" in "group.person.age: 41"	
			it.toNext();
			MS.syntaxAssertion(it.hasNext() && it.nextType() == NT_NAME_TOKEN, it, "name expected after a dot");
			it.toNext();

			// StructDef memberType = sem.getType((int)(memberTag & VALUE_TYPE_MASK), it);
			// memberTag = memberType.getMemberTagByName(it.data());
			
			currentStruct = sem.getType(memberType, it);
			
			MS.verbose("    DOT: " + it.data());
			
			if (isReference)
			{
				bc.code[lastOffsetCodeIndex] += currentStruct.getMemberAddressByName(it.data());
			}
			else
			{
				size = currentStruct.getMemberSizeByName(it.data());
				srcAddress += currentStruct.getMemberAddressByName(it.data()); // offset for the value
			}
			memberType = instrValueTypeID(currentStruct.getMemberTagByName(it.data()));
					
			arrayItemCount = -1;
			charCount = -1;
			if (memberType == MS_GEN_TYPE_ARRAY) {
				arrayItemCount = currentStruct.getMemberArrayItemCount(it.data());
			} else if (memberType == MS_GEN_TYPE_CHARS) {
				charCount = currentStruct.getCharCount(it.data());
			}
		}
		else if (it.hasNext() && it.nextType() == NT_SQUARE_BRACKETS)
		{
			MS.verbose("    SQ. BRACKETS");
			
			// e.g. "numbers[4]"
			
			MS.syntaxAssertion(memberType == MS_GEN_TYPE_ARRAY, it, "array expected");
			
			arrayItemCount = currentStruct.getMemberArrayItemCount(it.data());
			int arrayItemTypeID = currentStruct.getMemberArrayItemType(it.data());
			it.toNext();
				
			// array index
			it.toChild();
			MS.syntaxAssertion(!it.hasNext(), it, "array index expected");
			it.toChild();
			
			// get array item type
			StructDef arrayItemType = sem.getType(arrayItemTypeID, it);
			int itemSize = arrayItemType.structSize;
			
			if (it.type() == NT_NUMBER_TOKEN)
			{
				MS.syntaxAssertion(!it.hasNext(), it, "array index expected");
				
				// array index (number) expected");
				int arrayIndex = MS.parseInt(it.data().getString());
				// mul. size * index, and plus one as the array size is at [0]
				MS.syntaxAssertion(arrayIndex >= 0 && arrayIndex < arrayItemCount, it, "index out of range: " + arrayIndex + " of " + arrayItemCount);
				size = itemSize;
			
				if (isReference)
				{
					bc.code[lastOffsetCodeIndex] += itemSize * arrayIndex;
				}
				else
				{
					srcAddress += itemSize * arrayIndex;
				}
			}
			else
			{
				// handle variable (or other expression) array index
				// push index value
				NodeIterator cp = new NodeIterator(it);
				singleArgumentPush(MS_TYPE_INT, cp, -1);

				if (auxAddress < 0)
				{
					// create a auxiliar variable
					auxAddress = currentStruct.addMember(-1, MS_TYPE_INT, 1);
				}
				
				// write index value to variable
				
				bc.addInstruction(OP_MULTIPLY_GLOBAL_ARRAY_INDEX, 4, MS_TYPE_INT);
				bc.addWord(auxAddress);				// address to array index
				bc.addWord(itemSize);				// size of one array item
				
				lastOffsetCodeIndex = bc.codeTop;	// save the address to add offset later if needed
				
				if (isReference)
				{
					// tell MeanMachine that we want to add to the previous address as
					// this is not the first variable index of the chain,
					// e.g. "team[foo].position[bar]"
					bc.addWord(-1);
				}
				else
				{
					bc.addWord(srcAddress);			// address of the array data (size first)
				}
				bc.addWord(arrayItemCount);			// save item count to SYNTAX for array out-of-bounds
				
				// index will be save in the auxAddress now and it won't be changed here now.
				// instead add ADD operations to change the index, e.g. in "arr[foo].bar" would "bar" do.
				srcAddress = auxAddress;
				
				isReference = true;
				size = 1; // address size
			}
			
			// change the current struct
			currentStruct = arrayItemType;
			memberType = arrayItemType.typeID;
			arrayItemCount = -1;
			
			it.toParent();
			it.toParent();
		}
		else break;
	}
	
	if (isReference) { MS.assertion(size == 1,MC.EC_INTERNAL, ""); }		
	
	// arrays returns arrayItemCount > 0 and itemType as memberType
	MS.assertion(memberType != MS_GEN_TYPE_ARRAY,MC.EC_INTERNAL, "resolveMember can't return MS_GEN_TYPE_ARRAY");

	MS.verbose("RES. OUT size:" + size + " mtype:" + memberType + " addr:" + srcAddress + " arrC:" + arrayItemCount + " charCount:" + charCount + " ref:" + isReference);
	
	return new VarGen (size, memberType, srcAddress, arrayItemCount, charCount, isReference);
}

public void singleArgumentPush (int targetType, NodeIterator it, int arrayItemCount) 
{
	MS.verbose("Assign an argument [" + it.data() + "]");
	
	MS.assertion(targetType < MAX_TYPES,MC.EC_INTERNAL, "invalid type");
	
	if (it.type() == NT_EXPR)
	{
		MS.syntaxAssertion(!it.hasNext(), it, "argument syntax error");
		it.toChild();
		NodeIterator cp = new NodeIterator(it);
		singleArgumentPush(targetType, cp, arrayItemCount);
		it.toParent();
		return;
	}
	
	if (it.type() == NT_HEX_TOKEN)
	{
		if (targetType == MS_TYPE_INT)
		{
			long number = parseHex((it.data()).getString(), 8);
			bc.addInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_INT, int64lowBits(number));
			return;
		}
		else if (targetType == MS_TYPE_INT64)
		{
			long number = parseHex((it.data()).getString(), 16);
			bc.addInstruction(OP_PUSH_IMMEDIATE, 2, MS_TYPE_INT64);
			bc.addWord(int64highBits(number));
			bc.addWord(int64lowBits(number));
			return;
		}
		else
		{
			MS.syntaxAssertion(false, it, "number error");
		}
	}
	else if (it.type() == NT_NUMBER_TOKEN)
	{
		if (targetType == MS_TYPE_INT)
		{
			int number = MS.parseInt((it.data()).getString());
			bc.addInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_INT, number);
			return;
		}
		else if (targetType == MS_TYPE_INT64)
		{
			long number = MS.parseInt64((it.data()).getString());
			bc.addInstruction(OP_PUSH_IMMEDIATE, 2, MS_TYPE_INT64);
			bc.addWord(int64highBits(number));
			bc.addWord(int64lowBits(number));
			return;
		}
		else if (targetType == MS_TYPE_FLOAT)
		{
			float f = MS.parseFloat32(it.data().getString());
			int floatToInt = MS.floatToIntFormat(f);
			bc.addInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_FLOAT, floatToInt);
			return;
		}
		else if (targetType == MS_TYPE_FLOAT64)
		{
			double f = MS.parseFloat64((it.data()).getString());
			long number = MS.float64ToInt64Format(f);
			bc.addInstruction(OP_PUSH_IMMEDIATE, 2, MS_TYPE_FLOAT64);
			bc.addWord(int64highBits(number));
			bc.addWord(int64lowBits(number));
			return;
		}
		else
		{
			MS.syntaxAssertion(false, it, "number error");
		}
	}
	else if (it.type() == NT_TEXT)
	{
		int textID = tree.getTextID(it.data());
		MS.assertion(textID >= 0,MC.EC_INTERNAL, "text not found");
		if (targetType == MS_TYPE_TEXT)
		{
			// assign text id
			bc.addInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_TEXT, textID);
		}
		else
		{
			// copy chars
			int maxChars = arrayItemCount; // eg. 7 if "chars[7] x"
			// StructDef sd = sem.typeStructDefs[targetType];
			MS.syntaxAssertion(targetType == MS_GEN_TYPE_CHARS, it, "chars type expected");
			bc.addInstructionWithData(OP_PUSH_CHARS, 3, MS_TYPE_TEXT, textID);
			bc.addWord(maxChars);
			bc.addWord((maxChars/4) + 2 + 1); // characters + size
		}
		return;
	}
	else if (it.type() == NT_NAME_TOKEN)
	{
		Context functionContext = sem.findContext(it.data());
		if (functionContext != null)
		{
			// PUSH A FUNCTION ARGUMENT
			
			generateFunctionCall(it.copy(), functionContext);
			StructDef returnData = sem.getType(functionContext.returnType, it);
			MS.syntaxAssertion(targetType == returnData.typeID, it, "type mismatch");
			bc.addInstructionWithData(OP_PUSH_REG_TO_STACK, 1, MS_TYPE_VOID, returnData.structSize);
			return;
		}
		else if ((common.callbackIDs.ContainsKey( it.data())))
		{
			
			// PUSH A CALLBACK ARGUMENT
			
			MCallback callback = generateCallbackCall(it.copy());
			StructDef returnData = sem.getType(callback.returnType, it);
			MS.syntaxAssertion(targetType == returnData.typeID, it, "type mismatch");
			bc.addInstructionWithData(OP_PUSH_REG_TO_STACK, 1, MS_TYPE_VOID, returnData.structSize);
			return;
		}
		else 
		{
			// PUSH A VARIABLE ARGUMENT
			
			VarGen vg = resolveMember(it);

			// write the address or its reference from where to push

			if (vg.isReference)
			{
				bc.addInstruction(inGlobal() ? OP_PUSH_GLOBAL_REF : OP_PUSH_LOCAL_REF, 2, MS_TYPE_INT);
			}
			else
			{
				bc.addInstruction(inGlobal() ? OP_PUSH_GLOBAL : OP_PUSH_LOCAL, 2, MS_TYPE_INT);
			}
			
			bc.addWord(vg.address);
			bc.addWord(vg.size);
			
			MS.syntaxAssertion(targetType == vg.type, it, "type mismatch");

			return;
		}
	}
	else if (it.type() == NT_PARENTHESIS)
	{
		it.toChild();
		NodeIterator cp = new NodeIterator(it);
		singleArgumentPush(targetType, cp, arrayItemCount);
		it.toParent();
		return;
	}
	else if (it.type() == NT_REFERENCE_TOKEN)
	{
		// TODO: SYNTAX type
		int memberTag = currentContext.variables.getMemberTagByName(it.data());
		int address = currentContext.variables.getMemberAddressByName(it.data());
		int memberType = (int)(memberTag & VALUE_TYPE_MASK);
		bc.addInstructionWithData(OP_PUSH_IMMEDIATE, 1, memberType, address);
		return;
	}
	else if (it.type() == NT_CODE_BLOCK)
	{
		bc.addInstructionWithData(OP_JUMP,1,MS_TYPE_CODE_ADDRESS,-1); // -1 to be replaced after code block is done
		int jumpAddressPosition = bc.codeTop -1;
		int blockAddress = bc.codeTop;
			
		bc.addInstruction(OP_NOOP,0,0); // no-op in case that the block is empty

		// generate code block
		
		it.toChild();
		MS.assertion(it.type() == NT_EXPR,MC.EC_INTERNAL, "expression expected");
		MS.verbose(" - - - - start generating code block");
		generateCodeBlock(it);
		bc.addInstruction(OP_GO_BACK, 0 , 0);
		MS.verbose(" - - - - end generating code block");
		it.toParent();

		bc.code[jumpAddressPosition] = bc.codeTop;
		bc.addInstruction(OP_NOOP,0,0);
			
		bc.addInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_CODE_ADDRESS, blockAddress); // push the argument: block address
		return;
	}
	else if (it.type() == NT_SQUARE_BRACKETS)
	{
		if (targetType == MS_GEN_TYPE_ARRAY)
		{
			// push a list of array values
			
			it.toChild();
			arrayPush(it.copy(), targetType, arrayItemCount);
			it.toParent();
			return;
		}
		else
		{
			// push array values
			
			StructDef sd = sem.typeStructDefs[targetType];
			MS.syntaxAssertion(sd != null, it, "generator: unknown type: " + targetType);
			squareBracketArgumentPush(it.copy(), sd, sd.numMembers);
			return;
		}
	}
	else
	{
		MS.syntaxAssertion(false, it, "argument error");
	}
}


}
}
