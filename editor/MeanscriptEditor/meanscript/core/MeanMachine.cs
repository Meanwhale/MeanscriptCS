namespace Meanscript
{

	public class MeanMachine : MC
	{
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

		internal MHeap Heap = new MHeap();

		private static int currentStructID = 0;
		private static int textCounter = 0;

		public MeanMachine(ByteCode bc)
		{
			byteCode = bc;

			// INodeType.ARRAY_RESETtexts;
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

			Init();
		}
		//

		public bool IsInitialized()
		{
			return initialized;
		}

		public bool IsDone()
		{
			return done;
		}

		public void InitVMArrays()
		{
			// initialize arrays for code execution only if needed.

			MS.Assertion(!initialized, MC.EC_INTERNAL, "VM arrays must be initialized before code initialization is finished.");

			stack = new IntArray(MS.globalConfig.stackSize);
			ipStack = new IntArray(MS.globalConfig.ipStackSize);
			baseStack = new IntArray(MS.globalConfig.baseStackSize);
			functions = new IntArray(MS.globalConfig.maxFunctions);
			registerData = new IntArray(MS.globalConfig.registerSize);
		}

		public IntArray GetStructCode()
		{
			return byteCode.code;
		}

		public IntArray GetDataCode()
		{
			if (byteCodeType == BYTECODE_EXECUTABLE) return stack;
			else if (byteCodeType == BYTECODE_READ_ONLY) return byteCode.code;
			else MS.Assertion(false, MC.EC_INTERNAL, "wrong bytecode type");
			return null;
		}

		public void Gosub(int address)
		{
			int instruction = byteCode.code[address];
			MS.Assertion((instruction & OPERATION_MASK) == OP_NOOP, MC.EC_INTERNAL, "gosub: wrong address");
			PushIP(-instructionPointer); // gosub addresses (like when executing if's body) negative
			instructionPointer = address;
			jumped = true;
		}

		public void PushIP(int ip)
		{
			MS.Assertion(ipStackTop < MS.globalConfig.ipStackSize - 1, MC.EC_INTERNAL, "call stack overflow");
			ipStack[ipStackTop] = ip;
			ipStackTop++;
		}

		public int PopIP()
		{
			MS.Assertion(ipStackTop > 0, MC.EC_INTERNAL, "pop empty call stack");
			ipStackTop--;
			int rv = ipStack[ipStackTop];
			return rv < 0 ? -rv : rv; // return positive (gosub address is negative)
		}

		public int PopEndIP()
		{
			MS.Assertion(ipStackTop > 0, MC.EC_INTERNAL, "pop empty call stack");
			int rv = 0;
			do
			{
				ipStackTop--;
				rv = ipStack[ipStackTop];
			} while (rv < 0);
			return rv;
		}

		public bool Running()
		{
			return instructionPointer < byteCode.codeTop && !done;
		}

		public void StepToFunction(int id)
		{
			InitFunctionCall(id);
		}

		public void InitFunctionCall(int id)
		{
			MS.Assertion(initialized, EC_CODE, "Code not initialized.");
			MS.Verbose("Initialize a user's function call");
			ByteCode bc = byteCode;
			int tagAddress = functions[id];
			instructionPointer = bc.code[tagAddress + 2];
			stackBase = globalsSize;
			stackTop = globalsSize;
			done = false;
		}

		public void CallFunction(int id)
		{
			MS.Assertion(byteCodeType == BYTECODE_EXECUTABLE, MC.EC_INTERNAL, "bytecode is not executable");
			InitFunctionCall(id);
			while (Running())
			{
				Step();
			}
		}

		public int GetTextID(MSText text)
		{
			for (int i = 0; i < numTexts; i++)
			{
				if (IntStringsWithSizeEquals(text.GetData(), 0, byteCode.code, texts[i] + 1)) return i;
			}
			return -1;
		}

		public void Init()
		{
			MS.Assertion(!initialized, EC_CODE, "Code is already initialized.");
			MS.Assertion(byteCode.codeTop != 0, EC_CODE, "ByteCode is empty...");

			stackBase = 0;
			stackTop = 0;
			currentStructID = -1;
			textCounter = 1; // 0 is empty
			done = false;

			if (MS._verboseOn)
			{
				MS.Print(HORIZONTAL_LINE);
				MS.Print("START INITIALIZING");
				MS.Print(HORIZONTAL_LINE);
				PrintBytecode(byteCode.code, byteCode.codeTop, -1, true);
			}

			while (Running())
			{
				InitStep();
			}
			MS.Assertion(numTexts == textCounter, MC.EC_INTERNAL, "text count mismatch");
			MS.Verbose(HORIZONTAL_LINE);
			MS.Verbose("INITIALIZING FINISHED");
			MS.Verbose(HORIZONTAL_LINE);

			if (byteCodeType == BYTECODE_EXECUTABLE)
			{
				MS.Assertion(globalsSize == -1, MC.EC_INTERNAL, "EXEC. globals init error");
				int mainFunctionAddress = functions[0];
				globalsSize = byteCode.code[mainFunctionAddress + 3];
			}
			else
			{
				MS.Assertion(byteCodeType == BYTECODE_READ_ONLY && globalsSize >= 0, MC.EC_INTERNAL, "READ-ONLY globals init error");
			}

			initialized = true;
		}


		public void InitStep()
		{
			ByteCode bc = byteCode;

			// read instruction

			int instruction = bc.code[instructionPointer];
			MS.Verbose("INIT [" + GetOpName(instruction) + "]");
			//jumped = false;

			int op = (int)(instruction & OPERATION_MASK);

			if (instructionPointer == 0)
			{
				MS.Assertion(op == OP_START_INIT, EC_CODE, "bytecode starting tag missing");
				byteCodeType = (int)(instruction & AUX_DATA_MASK);
				MS.Assertion(byteCodeType == BYTECODE_READ_ONLY || byteCodeType == BYTECODE_EXECUTABLE, EC_CODE, "unknown bytecode type");
				numTexts = bc.code[instructionPointer + 1];
				MS.Verbose("start init! " + numTexts + " texts");
				texts = new IntArray(numTexts + 1);

				if (byteCodeType == BYTECODE_EXECUTABLE) InitVMArrays();
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
					MS.Assertion(globals == null, MC.EC_INTERNAL, "EXEC. globals already initialized");
					globalsTagAddress = instructionPointer;
					if (byteCodeType == BYTECODE_EXECUTABLE)
					{
						globals = new MSData(this, 0, 0);
					}
					else
					{
						MS.Assertion(byteCodeType == BYTECODE_READ_ONLY, MC.EC_INTERNAL, "unknown byteCodeType");
					}
				}
			}
			else if (op == OP_STRUCT_MEMBER) { }
			else if (op == OP_GENERIC_MEMBER) { }
			else if (op == OP_INIT_GLOBALS)
			{
				int size = InstrSize(instruction);

				if (byteCodeType == BYTECODE_EXECUTABLE)
				{
					// if read-only then the data is in bytecode, not stack
					stackBase = size;
					stackTop = size;
					for (int i = 1; i <= size; i++)
					{
						stack[i - 1] = bc.code[instructionPointer + i];
					}
				}
				else
				{
					MS.Assertion(globals == null, MC.EC_INTERNAL, "READ-ONLY globals already initialized");
					// for read-only data array is the bytecode array and start index is next to OP_INIT_GLOBALS tag.
					globals = new MSData(this, 0, instructionPointer + 1);
					globalsSize = size;
				}
			}
			else if (op == OP_END_INIT)
			{
				MS.Verbose("INIT DONE!");
				done = true;
			}
			else
			{
				MS.Assertion(false, EC_CODE, "unknown op. code");
			}
			instructionPointer += 1 + InstrSize(instruction);
		}


		public void Step()
		{
			ByteCode bc = byteCode;

			// read instruction

			int instruction = bc.code[instructionPointer];
			MS.Verbose("EXECUTE [" + GetOpName(instruction) + "]");
			jumped = false;

			int op = (int)(instruction & OPERATION_MASK);

			if (op == OP_PUSH_IMMEDIATE)
			{
				int size = InstrSize(instruction);
				// push words after the instruction to stack 
				for (int i = 1; i <= size; i++)
				{
					Push(bc.code[instructionPointer + i]);
				}
			}
			else if (op == OP_PUSH_GLOBAL)
			{
				int size = PopStack();
				int address = PopStack();

				//int address = bc.code[instructionPointer + 1];
				//int size = bc.code[instructionPointer + 2];

				PushData(stack, address, size);
			}
			else if (op == OP_PUSH_LOCAL)
			{
				int size = PopStack();
				int address = PopStack();
				//int address = bc.code[instructionPointer + 1];
				//int size = bc.code[instructionPointer + 2];

				PushData(stack, stackBase + address, size);
			}
			else if (op == OP_ADD_TOP)
			{
				int val = bc.code[instructionPointer + 1];
				stack[stackTop - 1] += val;
			}
			else if (op == OP_PUSH_GLOBAL_REF)
			{
				int refAddress = bc.code[instructionPointer + 1];
				int address = stack[refAddress];
				int size = bc.code[instructionPointer + 2];

				PushData(stack, address, size);
			}
			else if (op == OP_PUSH_LOCAL_REF)
			{
				int refAddress = bc.code[instructionPointer + 1];
				int address = stack[stackBase + refAddress];
				int size = bc.code[instructionPointer + 2];

				PushData(stack, address, size);
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
					textDataSize = InstrSize(bc.code[textIndex]);
					MS.Assertion(textChars <= maxChars, EC_CODE, "text too long");
					MS.Assertion(textDataSize <= structSize, EC_CODE, "text data too long");
					//MS.verbose("SIZE: " + textChars + " MAX: " + maxChars);

					PushData(bc.code, textIndex + 1, textDataSize);
				}
				// fill the rest
				for (int i = 0; i < (structSize - textDataSize); i++) Push(0);
			}
			else if (op == OP_POP_STACK_TO_GLOBAL || op == OP_POP_STACK_TO_LOCAL)
			{
				// stack: ... [target address][           data           ][size]

				// write from stack to global target

				int size = PopStack();
				int address = stack[stackTop - size - 1];
				PopStackToTarget(stack, size, address + (op == OP_POP_STACK_TO_LOCAL ? stackBase : 0));
				// pop address and check everything went fine
				MS.Assertion(address == PopStack(), EC_CODE, "OP_POP_STACK_TO_x failed");
			}
			else if (op == OP_POP_STACK_TO_DYNAMIC)
			{
				// stack: ... [dynamic target offset][           data           ][size]

				// write from stack to dynamic target

				int size = PopStack();
				int offset = stack[stackTop - size - 1];
				PopStackToTarget(Heap.Target.data, size, offset);
				// pop address and check everything went fine
				MS.Assertion(offset == PopStack(), EC_CODE, "OP_POP_STACK_TO_DYNAMIC failed");
				Heap.ClearTarget();
			}
			else if (op == OP_SET_DYNAMIC_OBJECT)
			{
				int dynamicPointersAddress = PopStack();
				int dynamicPointer;
				if (Heap.Target != null)
				{
					// get pointer from dynamic object's member
					dynamicPointer = Heap.Target.data[dynamicPointersAddress];
				}
				else
				{
					// get pointer from stack
					dynamicPointer = stack[dynamicPointersAddress];
				}
				Heap.SetTarget(dynamicPointer);
				Push(0);
			}
			else if (op == OP_POP_STACK_TO_GLOBAL_REF)
			{
				// write from stack to global reference target

				int size = bc.code[instructionPointer + 1];
				int refAddress = bc.code[instructionPointer + 2];
				int address = stack[refAddress];
				PopStackToTarget(stack, size, address);
			}
			else if (op == OP_POP_STACK_TO_LOCAL_REF)
			{
				// write from stack to local reference target

				int size = bc.code[instructionPointer + 1];
				int refAddress = bc.code[instructionPointer + 2];
				int address = stack[stackBase + refAddress];
				PopStackToTarget(stack, size, address);
			}
			else if (op == OP_POP_STACK_TO_REG)
			{
				// when 'return' is called
				int size = bc.code[instructionPointer + 1];
				PopStackToTarget(registerData, size, 0);
				registerType = (int)(instruction & VALUE_TYPE_MASK);
			}
			//else if (op == OP_MULTIPLY_GLOBAL_ARRAY_INDEX)
			//{
			//	// array index is pushed to the stack before
			//	// NOTE: works only for global arrays (check at Generator)

			//	int indexAddress = bc.code[instructionPointer + 1];
			//	int arrayItemSize = bc.code[instructionPointer + 2];
			//	int arrayDataAddress = bc.code[instructionPointer + 3];
			//	int arrayItemCount = bc.code[instructionPointer + 4];

			//	// get the index from stack top
			//	stackTop--;
			//	int arrayIndex = stack[stackTop];
			//	if (arrayIndex < 0 || arrayIndex >= arrayItemCount)
			//	{
			//		MS.errorOut.Print("ERROR: index " + arrayIndex + ", size" + arrayItemCount);
			//		MS.Assertion(false, EC_SCRIPT, "index out of bounds");
			//	}
			//	if (arrayDataAddress < 0)
			//	{
			//		// add address as this is not the first variable index of the chain,
			//		// e.g. "team[foo].position[bar]"
			//		stack[indexAddress] += (arrayIndex * arrayItemSize);
			//	}
			//	else
			//	{
			//		// save address to local variable for later use
			//		stack[indexAddress] = arrayDataAddress + (arrayIndex * arrayItemSize);
			//	}
			//}
			else if (op == OP_CALLBACK_CALL)
			{
				//public MeanMachine (ByteCode _byteCode, StructDef _structDef, int _base)
				int callbackIndex = (int)(instruction & VALUE_TYPE_MASK);
				CallbackType cb = bc.common.callbacks[callbackIndex];
				MS.Assertion(cb != null, MC.EC_INTERNAL, "invalid callback");
				int argsSize = cb.argStruct.StructSize();

				MArgs args = new MArgs(bc, cb.argStruct, stackTop - argsSize);

				MS.Verbose("-------- callback " + callbackIndex);
				PrintStack();
				cb.func(this, args);
				MS.Verbose("Clear stack after call");
				// clear stack after callback is done
				stackTop -= argsSize;
				PrintStack();
			}
			else if (op == OP_FUNCTION_CALL)
			{
				int functionID = bc.code[instructionPointer + 1];
				int tagAddress = functions[functionID];

				PushIP(instructionPointer); // save old IP
				instructionPointer = bc.code[tagAddress + 2];

				// args are already in stack. make room for locals
				int functionContextStructSize = bc.code[tagAddress + 3];
				int argsSize = bc.code[tagAddress + 4];
				int delta = functionContextStructSize - argsSize;
				for (int i = 0; i < delta; i++) stack[stackTop + i] = -1;

				stackTop += delta;
				stackBase = stackTop - functionContextStructSize;

				MS.Verbose("-------- function call! ID " + functionID + ", jump to " + instructionPointer);
				jumped = true;

			}
			else if (op == OP_SAVE_BASE)
			{
				MS.Verbose("-------- OP_PUSH_STACK_BASE");
				baseStack[baseStackTop++] = stackBase;
			}
			else if (op == OP_LOAD_BASE)
			{
				MS.Verbose("-------- OP_POP_STACK_BASE");
				baseStackTop--;
				stackTop = stackBase;
				stackBase = baseStack[baseStackTop];
			}
			else if (op == OP_PUSH_REG_TO_STACK)
			{
				MS.Assertion(registerType != -1, MC.EC_INTERNAL, "register empty");
				int size = bc.code[instructionPointer + 1];
				MS.Verbose("push register content to stack, size " + size);
				for (int i = 0; i < size; i++) Push(registerData[i]);
				registerType = -1;
			}
			else if (op == OP_JUMP)
			{
				int address = bc.code[instructionPointer + 1];
				MS.Verbose("jump: " + address);
				instructionPointer = address;
				jumped = true;
			}
			else if (op == OP_GO_BACK)
			{
				if (ipStackTop == 0)
				{
					MS.Verbose("DONE!");
					if (MS._verboseOn) Heap.Print();
					done = true;
				}
				else
				{
					MS.Verbose("Return from go-sub");
					// get the old IP and make a jump to the next instruction
					instructionPointer = PopIP();
					instruction = bc.code[instructionPointer];
					instructionPointer += 1 + InstrSize(instruction);
				}
				PrintStack();
				jumped = true;
			}
			else if (op == OP_GO_END)
			{
				MS.Verbose("Go to end of function");
				instructionPointer = PopEndIP();
				instruction = bc.code[instructionPointer];
				instructionPointer += 1 + InstrSize(instruction);
				jumped = true;
			}
			else if (op == OP_NOOP)
			{
				MS.Verbose(" . . . ");
			}
			else
			{
				throw new MException(MC.EC_INTERNAL, "unknown operation code");
			}

			//{if (MS.debug) {MS.verbose("STACK: base " + stackBase + ", top " + stackTop);}};
			//{if (MS.debug) {printData(stack, stackTop, stackBase, false);}};

			if (!jumped)
			{
				instructionPointer += 1 + InstrSize(instruction);
			}
		}


		public void PushData(IntArray source, int address, int size)
		{
			// push words from the source
			for (int i = 0; i < size; i++)
			{
				MS.Verbose("push from address " + (address + i));
				Push(source[address + i]);
			}
		}

		public int PopStack()
		{
			return stack[--stackTop];
		}

		public void PopStackToTarget(IntArray target, int size, int address)
		{
			for (int i = 0; i < size; i++)
			{
				target[address + i] = stack[stackTop - size + i];
				MS.Verbose("write " + stack[stackTop - size + i] + " to address " + (address + i));
			}
			MS.Verbose("clean stack");
			stackTop -= size;
		}

		public void Push(int data)
		{
			MS.Verbosen("push stack: " + data + "    ");
			stack[stackTop++] = data;
			PrintStack();
		}

		public void PrintStack()
		{
			MS.Verbosen("STACK:\n    ");
			if (MS._verboseOn)
			{
				for(int i=stackTop-1; i>=0; i--)
					if (i == stackBase) MS.Verbosen("/["+stack[i]+"]");
					else MS.Verbosen("/"+stack[i]);
			}
			MS.Verbose("");
		}

		public void CallbackReturn(int type, int value)
		{
			MS.Verbose(HORIZONTAL_LINE);
			MS.Verbose("        return " + value);
			MS.Verbose(HORIZONTAL_LINE);
			// return value from a callback
			// parameter for some other function or call
			SaveReg(type, value);
		}

		public void SaveReg(int type, int value)
		{
			registerType = type;
			registerData[0] = value;
		}


		public void WriteCode(MSOutputStream output)
		{
			byteCode.WriteCode(output);
		}

		public void WriteReadOnlyData(MSOutputStream output)
		{
			// write struct definitions
			byteCode.WriteStructInit(output);
			output.WriteInt(MakeInstruction(OP_INIT_GLOBALS, globalsSize, 0));
			for (int i = 0; i < globalsSize; i++)
			{
				output.WriteInt(stack[i]);
			}
			output.WriteInt(MakeInstruction(OP_END_INIT, 0, 0));
		}

		public void PrintGlobals()
		{
			MS.Print("GLOBALS: ");
			if (globalsSize == 0)
			{
				MS.Print("    <none>");
			}
			else
			{
				for (int i = 0; i < globalsSize; i++)
				{
					MS.Print("    " + i + ":    " + stack[i]);
				}
				MS.Print("");
			}
		}

		public void PrintDetails()
		{
			MS.Print("DETAILS");
			MS.Print(HORIZONTAL_LINE);
			MS.Print("globals size: " + globalsSize);
			MS.Print("stack base:   " + stackBase);
			MS.Print("stack top:    " + stackTop);
			MS.Print("call depth:   " + baseStackTop);
			MS.Print("instruction pointer: " + instructionPointer);

			MS.Print("\nSTACK");
			MS.Print(HORIZONTAL_LINE);
			PrintBytecode(stack, stackTop, -1, false);
		}

		public void PrintCode()
		{
			MS.Print("BYTECODE CONTENTS");
			MS.Print(HORIZONTAL_LINE);
			MS.Print("index     code/data (32 bits)");
			MS.Print(HORIZONTAL_LINE);
			PrintBytecode(byteCode.code, byteCode.codeTop, instructionPointer, true);
		}


		public void DataPrint()
		{
			MS.Print(HORIZONTAL_LINE);
			globals.PrintData(MS.printOut, 0, "");
			MS.Print(HORIZONTAL_LINE);
		}

	}
}
