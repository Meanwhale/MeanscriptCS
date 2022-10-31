namespace Meanscript {

public class MeanMachine : MC {
internal int stackTop;
internal int ipStackTop;
internal int baseStackTop;
internal int stackBase;
internal int instructionPointer;
internal int globalsSize;
internal int numTexts;
internal int registerType;
internal int byteCodeType;
internal bool initialized;
internal bool done;
internal bool jumped;
public IntArray stack;
internal IntArray ipStack;
internal IntArray baseStack;
internal IntArray functions;
internal IntArray registerData;
public IntArray texts;
public IntArray types; // typeID -> STRUCT_DEF tag address
public ByteCode byteCode;
public MSData globals;
internal int globalsTagAddress;



private static int currentStructID = 0;
private static int textCounter = 0;

public MeanMachine (ByteCode bc) 
{
	byteCode = bc;
	
	// INT_ARRAY_RESETtexts;
	types = new IntArray(MAX_TYPES);
	registerType = -1;
	byteCodeType = -1;
	ipStackTop = 0;
	baseStackTop = 0;
	stackBase = 0;
	globalsSize = -1;
	instructionPointer = 0; // instruction pointer
	globalsTagAddress = -1;
	initialized = false;
	done = false;
	jumped = false;
	globals = null;
	
	init();
}
//

public bool isInitialized ()
{
	return initialized;
}

public bool isDone ()
{
	return done;
}

public void initVMArrays () 
{	
	// initialize arrays for code execution only if needed.

	MS.assertion(!initialized,MC.EC_INTERNAL, "VM arrays must be initialized before code initialization is finished.");
	
	stack = new IntArray(MS.globalConfig.stackSize);
	ipStack = new IntArray(MS.globalConfig.ipStackSize);
	baseStack = new IntArray(MS.globalConfig.baseStackSize);
	functions = new IntArray(MS.globalConfig.maxFunctions);
	registerData = new IntArray(MS.globalConfig.registerSize);
}

public IntArray getStructCode ()
{
	return byteCode.code;
}

public IntArray getDataCode () 
{
	if (byteCodeType == BYTECODE_EXECUTABLE) return stack;
	else if (byteCodeType == BYTECODE_READ_ONLY) return byteCode.code;
	else MS.assertion(false,MC.EC_INTERNAL,"wrong bytecode type");
	return null;
}

public void gosub (int address) 
{
	int instruction = byteCode.code[address];
	MS.assertion((instruction & OPERATION_MASK) == OP_NOOP,MC.EC_INTERNAL, "gosub: wrong address");
	pushIP(-instructionPointer); // gosub addresses (like when executing if's body) negative
	instructionPointer = address;
	jumped = true;
}

public void pushIP (int ip) 
{
	MS.assertion(ipStackTop < MS.globalConfig.ipStackSize - 1,MC.EC_INTERNAL, "call stack overflow");
	ipStack[ipStackTop] = ip;
	ipStackTop++;
}

public int  popIP () 
{
	MS.assertion(ipStackTop > 0,MC.EC_INTERNAL, "pop empty call stack");
	ipStackTop--;
	int rv = ipStack[ipStackTop];
	return rv < 0 ? -rv : rv; // return positive (gosub address is negative)
}

public int  popEndIP () 
{
	MS.assertion(ipStackTop > 0,MC.EC_INTERNAL, "pop empty call stack");
	int rv=0;
	do {
		ipStackTop--;
		rv = ipStack[ipStackTop];
	} while (rv < 0);
	return rv;
}

public bool running ()
{
	return instructionPointer < byteCode.codeTop && !done;
}

public void stepToFunction (int id) 
{
	initFunctionCall(id);
}

public void initFunctionCall (int id) 
{
	MS.assertion(initialized, EC_CODE, "Code not initialized.");
	MS.verbose("Initialize a user's function call");
	ByteCode bc = byteCode;
	int tagAddress = functions[id];
	instructionPointer = bc.code[tagAddress + 2];
	stackBase = globalsSize;
	stackTop = globalsSize;
	done = false;
}

public void callFunction (int id) 
{
	MS.assertion(byteCodeType == BYTECODE_EXECUTABLE,MC.EC_INTERNAL, "bytecode is not executable");
	initFunctionCall(id);
	while (running())
	{
		step();
	}
}

public int  getTextID (MSText text)
{
	for (int i=0; i<numTexts; i++)
	{
		if (intStringsWithSizeEquals(text.getData(), 0, byteCode.code, texts[i] + 1)) return i;
	}
	return -1;
}

public void init () 
{
	MS.assertion(!initialized, EC_CODE, "Code is already initialized.");
	MS.assertion(byteCode.codeTop != 0, EC_CODE, "ByteCode is empty...");
	
	stackBase = 0;
	stackTop = 0;
	currentStructID = -1;
	textCounter = 1; // 0 is empty
	done = false;
	
	if (MS._verboseOn)
	{
		MS.print(HORIZONTAL_LINE);
		MS.print("START INITIALIZING");
		MS.print(HORIZONTAL_LINE);
		printBytecode(byteCode.code, byteCode.codeTop, -1, true);
	}

	while (running())
	{
		initStep();
	}
	MS.assertion(numTexts == textCounter,MC.EC_INTERNAL, "text count mismatch");
	MS.verbose(HORIZONTAL_LINE);
	MS.verbose("INITIALIZING FINISHED");
	MS.verbose(HORIZONTAL_LINE);
	
	if (byteCodeType == BYTECODE_EXECUTABLE)
	{
		MS.assertion(globalsSize == -1,MC.EC_INTERNAL, "EXEC. globals init error");
		int mainFunctionAddress = functions[0];
		globalsSize = byteCode.code[mainFunctionAddress + 3];
	}
	else
	{
		MS.assertion(byteCodeType == BYTECODE_READ_ONLY && globalsSize >= 0,MC.EC_INTERNAL, "READ-ONLY globals init error");
	}
	
	initialized = true;
}


public void initStep () 
{
	ByteCode bc = byteCode;
	
	// read instruction

	int instruction = bc.code[instructionPointer];
	MS.verbose("INIT [" + getOpName(instruction) + "]");
	//jumped = false;

	int op = (int)(instruction & OPERATION_MASK);

	if (instructionPointer == 0)
	{
		MS.assertion(op == OP_START_INIT, EC_CODE, "bytecode starting tag missing");
		byteCodeType = (int)(instruction & AUX_DATA_MASK);
		MS.assertion( 			byteCodeType == BYTECODE_READ_ONLY || 			byteCodeType == BYTECODE_EXECUTABLE, EC_CODE, "unknown bytecode type"); 
		numTexts = bc.code[instructionPointer + 1];
		MS.verbose("start init! " + numTexts + " texts");
		texts = new IntArray(numTexts+1);
		
		if (byteCodeType == BYTECODE_EXECUTABLE) initVMArrays();
	}
	else if (op == OP_ADD_TEXT)
	{
		// save address to this tag so the text can be found by its ID
		texts[textCounter++] = instructionPointer;
	}
	else if (op == OP_FUNCTION)
	{
		// FORMAT: | OP_FUNCTION | type | code address |
		int id = bc.code[instructionPointer + 1];
		functions[id] = instructionPointer; // save address to this tag 
	}
	//else if (op == OP_CHARS_DEF)
	//{
	//	currentStructID = (int)(instruction & VALUE_TYPE_MASK);
	//	types[currentStructID] = instructionPointer;
	//}
	else if (op == OP_STRUCT_DEF)
	{
		currentStructID = (int)(instruction & VALUE_TYPE_MASK);
		types[currentStructID] = instructionPointer;
		if (currentStructID == 0)
		{
			MS.assertion(globals == null,MC.EC_INTERNAL, "EXEC. globals already initialized");
			globalsTagAddress = instructionPointer;
			if (byteCodeType == BYTECODE_EXECUTABLE)
			{
				globals = new MSData(this, 0, 0);
			}
			else
			{
				MS.assertion(byteCodeType == BYTECODE_READ_ONLY,MC.EC_INTERNAL, "unknown byteCodeType");
			}
		}
	}
	else if (op == OP_STRUCT_MEMBER) { }
	else if (op == OP_GENERIC_MEMBER) { }
	else if (op == 	OP_INIT_GLOBALS)
	{
		int size = instrSize(instruction);
		
		if (byteCodeType == BYTECODE_EXECUTABLE)
		{
			// if read-only then the data is in bytecode, not stack
			stackBase = size;
			stackTop = size;
			for (int i=1; i<=size; i++)
			{
				stack[i-1] = bc.code[instructionPointer + i];
			}
		}
		else
		{
			MS.assertion(globals == null,MC.EC_INTERNAL, "READ-ONLY globals already initialized");
			// for read-only data array is the bytecode array and start index is next to OP_INIT_GLOBALS tag.
			globals = new MSData(this, 0, instructionPointer + 1);
			globalsSize = size;
		}
	}
	else if (op == OP_END_INIT)
	{
		MS.verbose("INIT DONE!");
		done = true;
	}
	else
	{
		MS.assertion(false, EC_CODE, "unknown op. code");
	}
	instructionPointer += 1 + instrSize(instruction);
}


public void step () 
{
	ByteCode bc = byteCode;
	
	// read instruction

	int instruction = bc.code[instructionPointer];
	MS.verbose("EXECUTE [" + getOpName(instruction) + "]");
	jumped = false;

	int op = (int)(instruction & OPERATION_MASK);

	if (op == OP_PUSH_IMMEDIATE)
	{
		int size = instrSize(instruction);
		// push words after the instruction to stack 
		for (int i=1; i<=size; i++)
		{
			push(bc.code[instructionPointer + i]);
		}
	}
	else if (op == OP_PUSH_GLOBAL)
	{
		int address = bc.code[instructionPointer + 1];
		int size    = bc.code[instructionPointer + 2];
		
		pushData(stack, address, size);
	}
	else if (op == OP_PUSH_LOCAL)
	{
		int address = bc.code[instructionPointer + 1];
		int size    = bc.code[instructionPointer + 2];
		
		pushData(stack, stackBase + address, size);
	}
	else if (op == OP_PUSH_GLOBAL_REF)
	{
		int refAddress = bc.code[instructionPointer + 1];
		int address = stack[refAddress];
		int size    = bc.code[instructionPointer + 2];
		
		pushData(stack, address, size);
	}
	else if (op == OP_PUSH_LOCAL_REF)
	{
		int refAddress = bc.code[instructionPointer + 1];
		int address = stack[stackBase + refAddress];
		int size    = bc.code[instructionPointer + 2];
		
		pushData(stack, address, size);
	}
	else if (op == OP_PUSH_CHARS)
	{
		int textID = bc.code[instructionPointer + 1];
		int maxChars = bc.code[instructionPointer + 2];
		int structSize = bc.code[instructionPointer + 3];
		int textDataSize = 0;
		if (textID != 0)
		{
			int textIndex = texts[textID];
			int textChars = bc.code[textIndex + 1];
			textDataSize = instrSize(bc.code[textIndex]);
			MS.assertion(textChars <= maxChars, EC_CODE, "text too long");
			MS.assertion(textDataSize <= structSize, EC_CODE, "text data too long");
			//MS.verbose("SIZE: " + textChars + " MAX: " + maxChars);
			
			pushData(bc.code, textIndex + 1, textDataSize);
		}
		// fill the rest
		for (int i=0; i < (structSize - textDataSize); i++) push(0);
	}
	else if (op == OP_POP_STACK_TO_GLOBAL)
	{
		// write from stack to global target
		
		int size = bc.code[instructionPointer + 1];
		int address = bc.code[instructionPointer + 2];
		popStackToTarget(bc, stack, size, address);
	}
	else if (op == OP_POP_STACK_TO_LOCAL)
	{
		// write from stack to local target
		
		int size = bc.code[instructionPointer + 1];
		int address = bc.code[instructionPointer + 2];
		popStackToTarget(bc, stack, size, address + stackBase);
	}
	else if (op == OP_POP_STACK_TO_GLOBAL_REF)
	{
		// write from stack to global reference target
		
		int size = bc.code[instructionPointer + 1];
		int refAddress = bc.code[instructionPointer + 2];
		int address = stack[refAddress];
		popStackToTarget(bc, stack, size, address);
	}
	else if (op == OP_POP_STACK_TO_LOCAL_REF)
	{
		// write from stack to local reference target
		
		int size = bc.code[instructionPointer + 1];
		int refAddress = bc.code[instructionPointer + 2];
		int address = stack[stackBase + refAddress];
		popStackToTarget(bc, stack, size, address);
	}
	else if (op == OP_POP_STACK_TO_REG)
	{
		// when 'return' is called
		int size = bc.code[instructionPointer + 1];
		popStackToTarget(bc, registerData, size, 0);
		registerType = (int)(instruction & VALUE_TYPE_MASK);
	}
	else if (op == OP_MULTIPLY_GLOBAL_ARRAY_INDEX)
	{
		// array index is pushed to the stack before
		// NOTE: works only for global arrays (check at Generator)
				
		int indexAddress		= bc.code[instructionPointer + 1];
		int arrayItemSize		= bc.code[instructionPointer + 2];
		int arrayDataAddress	= bc.code[instructionPointer + 3];
		int arrayItemCount		= bc.code[instructionPointer + 4];
				
		// get the index from stack top
		stackTop--;
		int arrayIndex = stack[stackTop];
		if (arrayIndex < 0 || arrayIndex >= arrayItemCount)
		{
			MS.errorOut.print("ERROR: index " + arrayIndex + ", size" + arrayItemCount);
			MS.assertion(false, EC_SCRIPT, "index out of bounds");
		}
		if (arrayDataAddress < 0)
		{
			// add address as this is not the first variable index of the chain,
			// e.g. "team[foo].position[bar]"
			stack[indexAddress] += (arrayIndex * arrayItemSize);
		}
		else
		{
			// save address to local variable for later use
			stack[indexAddress] = arrayDataAddress + (arrayIndex * arrayItemSize);
		}
	}
	else if (op == OP_CALLBACK_CALL)
	{
		//public MeanMachine (ByteCode _byteCode, StructDef _structDef, int _base)
		int callbackIndex = bc.code[instructionPointer + 1];
		MCallback cb = bc.common.callbacks[callbackIndex];
		MS.assertion(cb != null,MC.EC_INTERNAL, "invalid callback");
		int argsSize = cb.argStruct.structSize;
		
		MArgs args = new MArgs(bc, cb.argStruct, stackTop - argsSize);
		
		MS.verbose("-------- callback " + callbackIndex);
		cb.func( this, args);
		
		args = null;
		
		MS.verbose("Clear stack after call");
		// clear stack after callback is done
		stackTop -= cb.argStruct.structSize;
	}
	else if (op == OP_FUNCTION_CALL)
	{
		int functionID = bc.code[instructionPointer + 1];
		int tagAddress = functions[functionID];
		
		pushIP(instructionPointer); // save old IP
		instructionPointer = bc.code[tagAddress + 2];
		
		// args are already in stack. make room for locals
		int functionContextStructSize = bc.code[tagAddress + 3];
		int argsSize = bc.code[tagAddress + 4];
		int delta = functionContextStructSize - argsSize;
		for (int i=0; i<delta; i++) stack[stackTop+i] = -1;
		
		stackTop += delta;
		stackBase = stackTop - functionContextStructSize;
		
		MS.verbose("-------- function call! ID " + functionID + ", jump to " + instructionPointer);
		jumped = true;
		
	}
	else if (op == OP_SAVE_BASE)
	{
		MS.verbose("-------- OP_PUSH_STACK_BASE");
		baseStack[baseStackTop++] = stackBase;
	}
	else if (op == OP_LOAD_BASE)
	{
		MS.verbose("-------- OP_POP_STACK_BASE");
		baseStackTop--;
		stackTop = stackBase;
		stackBase = baseStack[baseStackTop];
	}
	else if(op == OP_PUSH_REG_TO_STACK)
	{
		MS.assertion(registerType != -1,MC.EC_INTERNAL, "register empty");
		int size = bc.code[instructionPointer + 1];
		MS.verbose("push register content to stack, size " + size);
		for (int i=0; i<size; i++) push(registerData[i]);
		registerType = -1;
	}
	else if (op == OP_JUMP)
	{
		int address = bc.code[instructionPointer + 1];
		MS.verbose("jump: " + address);
		instructionPointer = address;
		jumped = true;
	}
	else if (op == OP_GO_BACK)
	{
		if (ipStackTop == 0)
		{
			MS.verbose("DONE!");
			done = true;
		}
		else
		{
			MS.verbose("Return from go-sub");
			// get the old IP and make a jump to the next instruction
			instructionPointer = popIP();
			instruction = bc.code[instructionPointer];
			instructionPointer += 1 + instrSize(instruction);
		}
		jumped = true;
	}
	else if (op == OP_GO_END)
	{
		MS.verbose("Go to end of function");
		instructionPointer = popEndIP();
		instruction = bc.code[instructionPointer];
		instructionPointer += 1 + instrSize(instruction);
		jumped = true;
	}
	else if (op == OP_NOOP)
	{
		MS.verbose(" . . . ");
	}
	else
	{
		throw new MException(MC.EC_INTERNAL,"unknown operation code");
	}

	//{if (MS.debug) {MS.verbose("STACK: base " + stackBase + ", top " + stackTop);}};
	//{if (MS.debug) {printData(stack, stackTop, stackBase, false);}};

	if (!jumped)
	{
		instructionPointer += 1 + instrSize(instruction);
	}
}


public void pushData (IntArray source, int address, int size) 
{
	// push words from the source
	for (int i=0; i<size; i++)
	{
		MS.verbose("push from address " + (address + i));
		push(source[address + i]);
	}
}

public void popStackToTarget (ByteCode bc, IntArray target, int size, int address) 
{
	for (int i=0; i<size; i++)
	{
		target[address + i] = stack[stackTop - size + i];
		MS.verbose("write " + stack[stackTop - size + i] + " to address " + (address + i));
	}
	MS.verbose("clean stack");
	stackTop -= size;
}

public void push (int data) 
{
	MS.verbose("push stack: " + data);
	stack[stackTop++] = data;
}

public void callbackReturn (int type, int value) 
{
	MS.verbose(HORIZONTAL_LINE);
	     MS.verbose("        return " + value);
	MS.verbose(HORIZONTAL_LINE);
	// return value from a callback
	// parameter for some other function or call
	saveReg(type,value);
}

public void saveReg (int type, int value)
{
	registerType = type;
	registerData[0] = value;
}


public void  writeCode (MSOutputStream output) 
{
	byteCode.writeCode(output);
}

public void  writeReadOnlyData (MSOutputStream output) 
{
	// write struct definitions
	byteCode.writeStructInit(output);
	output.writeInt(makeInstruction(OP_INIT_GLOBALS, globalsSize, 0));
	for (int i=0; i<globalsSize; i++)
	{
		output.writeInt(stack[i]);
	}
	output.writeInt(makeInstruction(OP_END_INIT,0,0));
}

public void printGlobals() 
{
	MS.print("GLOBALS: ");
	if (globalsSize == 0)
	{
		MS.print("    <none>");
	}
	else
	{
		for (int i=0; i<globalsSize; i++)
		{
			MS.print("    " + i + ":    " + stack[i]);
		}
		MS.print("");
	}
}

public void printDetails() 
{
	MS.print("DETAILS");
	MS.print(HORIZONTAL_LINE);
	MS.print("globals size: " + globalsSize);
	MS.print("stack base:   " + stackBase);
	MS.print("stack top:    " + stackTop);
	MS.print("call depth:   " + baseStackTop);
	MS.print("instruction pointer: " + instructionPointer);
	
	MS.print("\nSTACK");
	MS.print(HORIZONTAL_LINE);
	printBytecode(stack, stackTop, -1, false);
}

public void printCode() 
{
	MS.print("BYTECODE CONTENTS");
	MS.print(HORIZONTAL_LINE);
	MS.print("index     code/data (32 bits)");
	MS.print(HORIZONTAL_LINE);
	printBytecode(byteCode.code, byteCode.codeTop, instructionPointer, true);
}


public void dataPrint() 
{
	MS.print(HORIZONTAL_LINE);
	globals.printData(MS.printOut, 0, "");
	MS.print(HORIZONTAL_LINE);
}

}
}
