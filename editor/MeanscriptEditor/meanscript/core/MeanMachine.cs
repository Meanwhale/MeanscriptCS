using System;

namespace Meanscript
{

	public class MeanMachine : MC
	{
		internal int stackTop;
		internal int ipStackTop;
		internal int baseStackTop;
		internal DData globalData;
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
		public CodeTypes codeTypes;
		public ByteCode byteCode;

		internal MHeap Heap = new MHeap();

		private static StructDefType currentStructDef = null;

		public MeanMachine(ByteCode bc)
		{
			byteCode = bc;

			// INodeType.ARRAY_RESETtexts;
			codeTypes = new CodeTypes(new Texts());
			
			// add common things to types
			// TODO: voiko jotenkin optimoida ettei tarvi aina lisätä näitä?
			//       ainakin jos on compile & run niin types pitäisi olla sama.
			//bc.common.Initialize();

			registerType = -1;
			byteCodeType = -1;
			ipStackTop = 0;
			baseStackTop = 0;
			stackBase = 0;
			globalsSize = -1;
			instructionPointer = 0; // instruction pointer
			initialized = false;
			done = false;
			jumped = false;

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

		internal string GetText(int id)
		{
			if (id <= 0) return "";
			int address = texts[id];
			int numChars = byteCode.code[address + 1];
			string s = System.Text.Encoding.UTF8.GetString(MS.IntsToBytes(byteCode.code, address + 2, numChars));
			return s;
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
			//stackBase = globalsSize;
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

		public void Init()
		{
			MS.Assertion(!initialized, EC_CODE, "Code is already initialized.");
			MS.Assertion(byteCode.codeTop != 0, EC_CODE, "ByteCode is empty...");

			stackBase = 0;
			stackTop = 0;
			currentStructDef = null;
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
			MS.Verbose(HORIZONTAL_LINE);
			MS.Verbose("INITIALIZING FINISHED");
			MS.Verbose(HORIZONTAL_LINE);
			
			foreach(var t in codeTypes.types.Values)
			{
				t.Init(codeTypes);
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
				globalsSize = bc.code[instructionPointer + 2];
				MS.Verbose("start init! " + numTexts + " texts");
				texts = new IntArray(numTexts + 1);
				
				globalData = Heap.AllocGlobal(globalsSize);

				if (byteCodeType == BYTECODE_EXECUTABLE) InitVMArrays();
			}
			else if (op == OP_ADD_TEXT)
			{
				// save address to this tag so the text can be found by its ID
				int textID = (int)(instruction & AUX_DATA_MASK);
				texts[textID] = instructionPointer;
				var s = GetText(textID);
				codeTypes.texts.AddText(textID, s);
				MS.Verbose("new text [" + textID + "] " + s);
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
				// add a struct and set it current for coming members (next op.)
				int structID = (int)(instruction & VALUE_TYPE_MASK);
				int nameID = bc.code[instructionPointer + 1];
				MS.Verbose("new struct def: " + GetText(nameID));
				var sd = new StructDef(codeTypes, nameID);
				currentStructDef = new StructDefType(structID, codeTypes.texts.GetText(nameID), sd);
				codeTypes.AddTypeDef(currentStructDef);
			}
			else if (op == OP_STRUCT_MEMBER)
			{
				int structID = (int)(instruction & VALUE_TYPE_MASK);
				MS.Assertion(structID == currentStructDef.ID);

				int nameID   = bc.code[instructionPointer + 1];
				int typeID   = bc.code[instructionPointer + 2];
				int refID    = bc.code[instructionPointer + 3];
				int address  = bc.code[instructionPointer + 4];
				int datasize = bc.code[instructionPointer + 5];
				int index    = bc.code[instructionPointer + 6];
				
				MS.Verbose("new struct member. struct: " + structID +
					"\nname:  " + (nameID < 1 ? nameID.ToString() : GetText(nameID)) +
					"\ntype:  " + typeID +
					"\nref:   " + refID +
					"\naddr:  " + address +
					"\nsize:  " + datasize + 
					"\nindex: " + index);

				var td = codeTypes.GetType(typeID);
				MS.Assertion(td != null, MC.EC_CODE, "type not found by ID " + typeID);
				currentStructDef.SD.AddMember(nameID, td, refID, address, datasize, index);
			}
			else if (op == OP_GENERIC_TYPE)
			{
				MS.Verbose("new generic type");
				int genCodeID = (int)(instruction & VALUE_TYPE_MASK);
				int numArgs = InstrSize(instruction);
				MS.Verbose("code: " + genCodeID + " args: " + numArgs);
				var fac = GenericFactory.Get(genCodeID);
				var args = new int[numArgs];

				MS.Verbose("    args:");
				for (int i=0; i<numArgs; i++)
				{
					args[i] = bc.code[instructionPointer + i + 1];
					MS.Verbose("    " + args[i]);
				}				
				codeTypes.AddTypeDef(fac.Decode(this, args));
			}
			else if (op == OP_INIT_GLOBALS)
			{
				int size = InstrSize(instruction);

				if (byteCodeType == BYTECODE_EXECUTABLE)
				{
					// if read-only then the data is in bytecode, not stack
					stackBase = 0;
					stackTop = 0;
					globalData = Heap.AllocGlobal(size);
				}
				else
				{
					MS.Assertion(false);
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

			if (bc.nodes.ContainsKey(instructionPointer))
			{
				var node = bc.nodes[instructionPointer];
				MS.Verbose("\n %%%%%%%%%%%%%%%% line " + node.lineNumber + " ch. " + node.characterNumber + "\n");
			}

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
			else if (op == OP_PUSH_OBJECT_DATA)
			{
				int size = PopStack();
				int address = PopStack();

				// int address = bc.code[instructionPointer + 1];
				// int size = bc.code[instructionPointer + 2];
				// PushData(stack, address, size);
				PushData(Heap.GetDataArray(MC.AddressHeapID(address)), MC.AddressOffset(address), size);
			}
			else if (op == OP_PUSH_LOCAL)
			{
				MS.Assertion(false, MC.EC_INTERNAL, "functions will have own objects");
				//int size = PopStack();
				//int address = PopStack();
				//PushData(stack, stackBase + address, size);
			}
			else if (op == OP_ADD_STACK_TOP_ADDRESS_OFFSET)
			{
				int val = PopStack();
				int address = stack[stackTop - 1];
				address = MC.MakeAddress(MC.AddressHeapID(address), MC.AddressOffset(address + val));
				stack[stackTop - 1] = address;
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
			else if (op == OP_POP_STACK_TO_OBJECT || op == OP_POP_STACK_TO_OBJECT_TAG)
			{
				// stack: ... [target address][           data           ][size]

				// write from stack to object

				int size = PopStack();
				int address = stack[stackTop - size - 1];
				int heapID = MC.AddressHeapID(address);
				int offset = MC.AddressOffset(address);
				MS.Assertion(Heap.HasObject(heapID), MC.EC_CODE, "address' heap ID error: " + heapID);
				
				if (op == OP_POP_STACK_TO_OBJECT_TAG)
				{
					int tag = Heap.GetAt(heapID, offset);
					if (MS._verboseOn) MS.printOut.Print("object tag: ").PrintHex(tag).EndLine();
					if (tag != 0) Heap.Free(tag, -1);
				}

				var targetArray = Heap.GetDataArray(heapID);
				MS.Assertion(targetArray.Length >= offset + size, MC.EC_CODE, "writing over bounds. object size: " + targetArray.Length + ", offset: " + offset + ", size: " + size);
				PopStackToTarget(targetArray, size, offset);
				// pop address and check everything went fine
				MS.Assertion(address == PopStack(), EC_CODE, "OP_POP_STACK_TO_x failed");
			}
			else if (op == OP_SET_DYNAMIC_OBJECT)
			{
				// pop the dynamic object HEAP address from the stack top
				// and push it's address (head ID + offset 0) to top of the stack
				
				int address = PopStack();
				int heapID = MC.AddressHeapID(address);
				int offset = MC.AddressOffset(address);

				int tag = Heap.GetAt(heapID, offset);
				int newHeapID = MHeap.TagIndex(tag);
				MS.Assertion(Heap.HasObject(newHeapID), MC.EC_CODE, "address' heap ID error: " + newHeapID);
				Push(MC.MakeAddress(newHeapID, 0));

				MS.Verbose("set dynamic object address, newHeapID: " + newHeapID + ", offset: " + offset);
			}
			else if (op == OP_CALLBACK_CALL)
			{
				//public MeanMachine (ByteCode _byteCode, StructDef _structDef, int _base)
				int callbackIndex = (int)(instruction & VALUE_TYPE_MASK);
				MS.Assertion(codeTypes.HasCallback(callbackIndex), MC.EC_CODE, "unknown callback, id: " + callbackIndex);
				CallbackType cb = codeTypes.GetCallback(callbackIndex); // bc.common.callbacks[callbackIndex];
				MS.Assertion(cb != null, MC.EC_INTERNAL, "invalid callback");
				//int argsSize = cb.argStruct.StructSize();

				MArgs args = new MArgs(bc, cb.argStruct, stackTop - cb.argsSize);

				MS.Verbose("callback: " + callbackIndex + " argsSize: " + cb.argsSize);
				PrintStack();
				cb.func(this, args);
				MS.Verbose("Clear stack after call");
				// clear stack after callback is done
				stackTop -= cb.argsSize;
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
			MS.Printn("STACK:\n    ");
			if (MS._verboseOn)
			{
				MS.Printn("<base>");
				for(int i=0; i<stackTop; i++)
					if (i == stackBase) MS.printOut.Print("/[").PrintHex(stack[i]).Print("]");
					else MS.printOut.Print("/").PrintHex(stack[i]);
				MS.Printn("/<top>");
			}
			MS.Print("");
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
				output.WriteInt(globalData.data[i]);
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
					MS.Print("    " + i + ":    " + globalData.data[i]);
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
	}
}
