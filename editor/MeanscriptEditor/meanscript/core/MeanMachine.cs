using System.Collections.Generic;

namespace Meanscript.Core
{

	public class MeanMachine
	{
		internal class CallBase
		{
			public CallBase(DData heapObject) { HeapObject = heapObject; }
			public DData HeapObject;
		}

		internal int stackTop;
		internal int ipStackTop;
		internal DData currentContextData;
		//internal int stackBase;
		//internal int instructionPointer;
		internal int registerType;
		internal bool initialized;
		internal bool done;
		internal bool jumped;
		public IntArray stack;
		internal IntArray ipStack;
		internal MList<CallBase> contextStack = new MList<CallBase>();
		internal IntArray functions;
		internal IntArray registerData;
		private MSInput input;
		public CodeTypes codeTypes;
		private Texts texts;

		internal MHeap Heap = new MHeap();

		private static StructDefType currentStructDef = null;
		private Dictionary<int, MNode> nodes;

		public MeanMachine(MSInput _input, Dictionary<int, MNode> _nodes = null)
		{
			if (_nodes == null) nodes = new Dictionary<int, MNode>();
			else nodes = _nodes;

			input = _input;
			
			texts = new Texts();
			// INodeType.ARRAY_RESETtexts;
			codeTypes = new CodeTypes(texts);
			
			// add common things to types
			// TODO: voiko jotenkin optimoida ettei tarvi aina lisätä näitä?
			//       ainakin jos on compile & run niin types pitäisi olla sama.
			//bc.common.Initialize();

			registerType = -1;
			ipStackTop = 0;
			//instructionPointer = 0; // instruction pointer
			initialized = false;
			done = false;
			jumped = false;
			
			InitVMArrays();

			Run();
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
			functions = new IntArray(MS.globalConfig.maxFunctions);
			registerData = new IntArray(MS.globalConfig.registerSize);
		}

		internal string FindTextByID(int id)
		{
			return texts.FindTextStringByID(id);
		}

		//public void Gosub(int address)
		//{
		//	int instruction = byteCode.code[address];
		//	MS.Assertion((instruction & MC.OPERATION_MASK) == MC.OP_NOOP, MC.EC_INTERNAL, "gosub: wrong address");
		//	PushIP(-instructionPointer); // gosub addresses (like when executing if's body) negative
		//	instructionPointer = address;
		//	jumped = true;
		//}

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
			return !done;
		}

		public void StepToFunction(int id)
		{
			InitFunctionCall(id);
		}

		public void InitFunctionCall(int id)
		{
			MS.Assertion(false);
			//MS.Assertion(initialized, MC.EC_CODE, "Code not initialized.");
			//MS.Verbose("initialize a user's function call");
			//int tagAddress = functions[id];
			//MS.Assertion(tagAddress != 0, MC.EC_CODE, "no function with ID " + id);
			//instructionPointer = bc.code[tagAddress + 2];
			////stackBase = globalsSize;
			//done = false;
		}

		public void CallFunction(int id)
		{
			InitFunctionCall(id);
			while (Running())
			{
				Step();
			}
		}

		public void Run()
		{
			MS.Assertion(!initialized, MC.EC_CODE, "Code is already initialized.");

			stackTop = 0;
			currentStructDef = null;
			done = false;

			if (MS._verboseOn)
			{
				MS.Print(MS.Title("START INITIALIZING"));
				MS.Print(MS.Title("TODO PrintBytecode"));
				PrintCode();
			}

			while (Running())
			{
				Step();
			}
			MS.Verbose(MS.Title("INITIALIZING FINISHED"));
			
			foreach(var t in codeTypes.types.Values)
			{
				t.Init(codeTypes);
			}
			initialized = true;
		}


		public void Step()
		{
			// read instruction

			if (MS._verboseOn && nodes != null && input is MSInputArray ia)
			{
				if (nodes.ContainsKey(ia.IntIndex()))
				{
					var node = nodes[ia.IntIndex()];
					MS.Print(MS.Title("script line " + node.lineNumber + " ch. " + node.characterNumber));
				}
			}

			int instruction = input.ReadInt();

			MS.Verbose("EXECUTE [" + MC.GetOpName(instruction) + "]");
			jumped = false;

			int op = (int)(instruction & MC.OPERATION_MASK);

			if (op == MC.OP_START_DEFINE)
			{
				//MS.Assertion(op == MC.OP_START_DEFINE, MC.EC_CODE, "bytecode starting tag missing");
				//numTexts = bc.code[instructionPointer + 1];
				MS.Verbose("start init!");
				//texts = new IntArray(numTexts + 1);
			}
			else if (op == MC.OP_ADD_TEXT)
			{
				// read text from input and save it by ID
				
				int textID = (int)(instruction & MC.AUX_DATA_MASK);
				var t = new MSText(input);
				texts.AddText(textID, t);
				MS.Verbose("new text [" + textID + "] " + t);
			}
			else if (op == MC.OP_FUNCTION)
			{
				MS.Assertion(false); // functiot muuttuu
				//// FORMAT: | MC.OP_FUNCTION | type | code address |
				//int id = bc.code[instructionPointer + 1];
				//functions[id] = instructionPointer; // save address to this tag 
			}
			else if (op == MC.OP_STRUCT_DEF)
			{
				// add a struct and set it current for coming members (next op.)
				int structID = (int)(instruction & MC.VALUE_TYPE_MASK);
				//int nameID = bc.code[instructionPointer + 1];
				int nameID = input.ReadInt();
				MS.Verbose("new struct def., id: " + structID + " " + FindTextByID(nameID));
				var sd = new StructDef(codeTypes, nameID);
				currentStructDef = new StructDefType(structID, codeTypes.texts.GetText(nameID), sd);
				codeTypes.AddTypeDef(currentStructDef);
			}
			else if (op == MC.OP_STRUCT_MEMBER)
			{
				int structID = (int)(instruction & MC.VALUE_TYPE_MASK);
				MS.Assertion(structID == currentStructDef.ID);
				
				int nameID   = input.ReadInt();	// + 1
				int typeID   = input.ReadInt();	// + 2
				int refID    = input.ReadInt();	// + 3
				int address  = input.ReadInt();	// + 4
				int datasize = input.ReadInt();	// + 5
				int index    = input.ReadInt();	// + 6
				
				MS.Verbose("new struct member. struct: " + structID +
					"\n    name:  " + (nameID < 1 ? nameID.ToString() : FindTextByID(nameID)) +
					"\n    type:  " + typeID +
					"\n    ref:   " + refID +
					"\n    addr:  " + address +
					"\n    size:  " + datasize + 
					"\n    index: " + index);

				var td = codeTypes.GetTypeDef(typeID);
				MS.Assertion(td != null, MC.EC_CODE, "type not found by ID " + typeID);
				currentStructDef.SD.AddMember(nameID, td, refID, address, datasize, index);
			}
			else if (op == MC.OP_GENERIC_TYPE)
			{
				int genCodeID = (int)(instruction & MC.VALUE_TYPE_MASK);
				int numArgs = MC.InstrSize(instruction);
				
				var fac = GenericFactory.Get(genCodeID);
				var args = new int[numArgs];

				MS.Verbose("new generic type, code: " + genCodeID + ", args (" + numArgs + "):");
				for (int i=0; i<numArgs; i++)
				{
					args[i] = input.ReadInt();
					// args[i] = bc.code[instructionPointer + i + 1];
					MS.Verbose("    " + args[i]);
				}				
				codeTypes.AddTypeDef(fac.Decode(this, args));
			}
			else if (op == MC.OP_WRITE_HEAP_OBJECT)
			{
				// [ OP > HeapID > ... data ... ] size = data size + 1 (heapID)

				int dataSize = MC.InstrSize(instruction) - 1;
				int typeID = (int)(instruction & MC.VALUE_TYPE_MASK);
				//int heapID = bc.code[instructionPointer + 1];
				int heapID = input.ReadInt();

				// copy data from instruction's bytecode to heap

				Heap.ReadFromInput(
					heapID == 1 ? DData.Role.GLOBAL : DData.Role.OBJECT,
					heapID,
					typeID,
					input,
					dataSize);
			}
			else if (op == MC.OP_END_INIT)
			{
				MS.Verbose("INIT DONE!");
				done = true;
			}
			else if (op == MC.OP_START_INIT)
			{
				MS.Verbose("INITIALIZE GLOBALS");

				// create global data object if needed
				if (!Heap.HasObject(1))
				{
					var gl = codeTypes.GetTypeDef(MC.GLOBALS_TYPE_ID);
					Heap.AllocGlobal(gl.SizeOf());
				}
				currentContextData = Heap.GetDDataByIndex(1);
			}

			//////////////////////////////// OLD RUN STEP OPS ////////////////////////////////

			else if (op == MC.OP_PUSH_IMMEDIATE)
			{
				int size = MC.InstrSize(instruction);
				// push words after the instruction to stack 
				for (int i = 1; i <= size; i++)
				{
					//Push(bc.code[instructionPointer + i]);
					Push(input.ReadInt());
				}
			}
			else if (op == MC.OP_PUSH_CONTEXT_ADDRESS)
			{
				//int offset = bc.code[instructionPointer + 1];
				int offset = input.ReadInt();
				Push(MC.MakeAddress(currentContextData.HeapID(), offset));
			}
			else if (op == MC.OP_PUSH_OBJECT_DATA)
			{
				int size = PopStack();
				int address = PopStack();
				PushData(Heap.GetDataArray(MC.AddressHeapID(address)), MC.AddressOffset(address), size);
			}
			else if (op == MC.OP_ADD_STACK_TOP_ADDRESS_OFFSET)
			{
				int val = PopStack();
				int address = stack[stackTop - 1];
				address = MC.MakeAddress(MC.AddressHeapID(address), MC.AddressOffset(address + val));
				stack[stackTop - 1] = address;
			}
			else if (op == MC.OP_PUSH_CHARS)
			{
				//int textID = bc.code[instructionPointer + 1];
				//int maxChars = bc.code[instructionPointer + 2];
				//int structSize = bc.code[instructionPointer + 3];
				int textID = input.ReadInt();
				int maxBytes = input.ReadInt();
				int structSize = input.ReadInt();
				int textDataSize = 0;
				if (textID != 0)
				{
					// get text data from the texts

					var t = texts.GetTextByID(textID);
					MS.Assertion(t != null, MC.EC_CODE, "wrong text ID: " + textID);
					textDataSize = t.DataSize();
					MS.Assertion(t.NumBytes() <= maxBytes, MC.EC_CODE, "text too long");
					MS.Assertion(textDataSize  <= structSize, MC.EC_CODE, "text data too long");
					PushData(t.GetData(), 0, textDataSize);
				}
				// fill the rest
				for (int i = 0; i < (structSize - textDataSize); i++) Push(0);
			}
			else if (op == MC.OP_POP_STACK_TO_OBJECT || op == MC.OP_POP_STACK_TO_OBJECT_TAG)
			{
				// stack: ... [target address][           data           ][size]

				// write from stack to object

				int size = PopStack();
				int address = stack[stackTop - size - 1];
				int heapID = MC.AddressHeapID(address);
				int offset = MC.AddressOffset(address);
				MS.Assertion(Heap.HasObject(heapID), MC.EC_CODE, "address' heap ID error: " + heapID);
				
				if (op == MC.OP_POP_STACK_TO_OBJECT_TAG)
				{
					int tag = Heap.GetAt(heapID, offset);
					if (MS._verboseOn) MS.printOut.Print("object tag: ").PrintHex(tag).EndLine();
					if (tag != 0) Heap.Free(tag, -1);
				}

				var targetArray = Heap.GetDataArray(heapID);
				MS.Assertion(targetArray.Length >= offset + size, MC.EC_CODE, "writing over bounds. targetArray size: " + targetArray.Length + ", offset: " + offset + ", data size: " + size);
				PopStackToTarget(targetArray, size, offset);
				// pop address and check everything went fine
				MS.Assertion(address == PopStack(), MC.EC_CODE, "MC.OP_PMC.OP_STACK_TO_x failed");
			}
			else if (op == MC.OP_SET_DYNAMIC_OBJECT)
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
			else if (op == MC.OP_CALLBACK_CALL)
			{
				//public MeanMachine (ByteCode _byteCode, StructDef _structDef, int _base)
				int callbackIndex = (int)(instruction & MC.VALUE_TYPE_MASK);
				MS.Assertion(codeTypes.HasCallback(callbackIndex), MC.EC_CODE, "unknown callback, id: " + callbackIndex);
				CallbackType cb = codeTypes.GetCallback(callbackIndex); // bc.common.callbacks[callbackIndex];
				MS.Assertion(cb != null, MC.EC_INTERNAL, "invalid callback");
				//int argsSize = cb.argStruct.StructSize();

				MArgs args = new MArgs(cb, stackTop - cb.argsSize);

				MS.Verbose("callback: " + callbackIndex + " argsSize: " + cb.argsSize);
				PrintStack();
				cb.func(this, args);
				MS.Verbose("clear stack after call");
				// clear stack after callback is done
				stackTop -= cb.argsSize;
				PrintStack();
			}
			else if (op == MC.OP_FUNCTION_CALL)
			{
				MS.Assertion(false); // refactor

				/*int functionID = bc.code[instructionPointer + 1];
				int tagAddress = functions[functionID];

				PushIP(instructionPointer); // save old IP
				instructionPointer = bc.code[tagAddress + 2];

				// args are already in stack. make room for locals
				int functionContextStructSize = bc.code[tagAddress + 3];
				int argsSize = bc.code[tagAddress + 4];

				// luo data function contextille (heap object) ja tall. vanha pinoon

				contextStack.AddFirst(new CallBase(currentContextData));
				currentContextData = Heap.AllocContext(functionContextStructSize);

				// poppaa argsit heap objectiin. object datan alkupää on varattu function argumenteille.
				PopStackToTarget(currentContextData.data, argsSize, 0);

				MS.Verbose(MS.Title("function call! ID " + functionID + ", jump to " + instructionPointer + ", function context heap ID " + currentContextData.HeapID()));
				jumped = true;*/

			}
			else if (op == MC.OP_RETURN_FUNCTION)
			{
				MS.Assertion(false); // refactor

				//if (ipStackTop == 0)
				//{
				//	MS.Verbose("DONE!");
				//	if (MS._verboseOn) Heap.Print();
				//	done = true;
				//	//MS.Assertion(stackTop == 0); // TODO: pitäisikö olla?
				//}
				//else
				//{
				//	// get the old IP and make a jump to the next instruction
					
				//	instructionPointer = PopIP();
				//	instruction = bc.code[instructionPointer];
				//	instructionPointer += 1 + MC.InstrSize(instruction);
					
				//	// free current and get previous function context

				//	Heap.FreeContext(currentContextData);
				//	currentContextData = contextStack.First().HeapObject;
				//	contextStack.RemoveFirst();
				//}
				//PrintStack();
				//jumped = true;
			}
			else if (op == MC.OP_JUMP)
			{
				MS.Assertion(false); // refactor

				////int address = bc.code[instructionPointer + 1];
				//int address = input.ReadInt();
				//MS.Verbose("jump: " + address);
				//instructionPointer = address;
				//jumped = true;
			}
			else if (op == MC.OP_GO_END)
			{
				MS.Assertion(false); // refactor

				//instructionPointer = PopEndIP();
				//instruction = bc.code[instructionPointer];
				//instructionPointer += 1 + MC.InstrSize(instruction);
				//MS.Verbose("go to end of function, instructionPointer: " + instructionPointer);
				//jumped = true;
			}
			else if (op == MC.OP_POP_STACK_TO_REG)
			{
				// when 'return' is called
				//int size = bc.code[instructionPointer + 1];
				int size = input.ReadInt();
				PopStackToTarget(registerData, size, 0);
				registerType = (int)(instruction & MC.VALUE_TYPE_MASK);
			}
			else if (op == MC.OP_PUSH_REG_TO_STACK)
			{
				MS.Assertion(registerType != -1, MC.EC_INTERNAL, "register empty: return value was expected");
				//int size = bc.code[instructionPointer + 1];
				int size = input.ReadInt();
				MS.Verbose("push register content to stack, size " + size);
				for (int i = 0; i < size; i++) Push(registerData[i]);
				registerType = -1;
			}
			else if (op == MC.OP_NOOP)
			{
				MS.Verbose(" . . . ");
			}
			else
			{
				MS.errorOut.Print("operation code: ").PrintHex(op);
				throw new MException(MC.EC_INTERNAL, "unknown operation");
			}

			//if (!jumped)
			//{
			//	instructionPointer += 1 + MC.InstrSize(instruction);
			//}
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
			MS.Verbosen("push stack: " + data + ". ");
			stack[stackTop++] = data;
			PrintStack();
		}

		public void PrintStack()
		{
			MS.Verbosen("STACK:\n    ");
			if (MS._verboseOn)
			{
				if (stackTop == 0)
				{
					MS.Verbose("//empty");
					return;
				}
				for(int i=0; i<stackTop; i++)
					MS.printOut.Print("/").PrintHex(stack[i]);
				MS.Verbosen("//top");
			}
			MS.Verbose("");
		}

		public void CallbackReturn(int type, int value)
		{
			MS.Verbose(MS.Title("return " + value));
			// return value from a callback
			// parameter for some other function or call
			SaveReg(type, value);
		}

		public void SaveReg(int type, int value)
		{
			registerType = type;
			registerData[0] = value;
		}

		public void GenerateDataCode(MSOutput output)
		{
			// not copy because bytecode won't be saved in the future.
			// code will be executed from stream.
			// then it will be similar to MSBuilder
			
			output.WriteOp(MC.OP_START_DEFINE, 0, 0);

			// add texts
			foreach (var textEntry in texts.texts)
			{
				MC.WriteTextInstruction(output, textEntry.Key, MC.OP_ADD_TEXT, textEntry.Value);
			}

			codeTypes.WriteTypes(output);
			
			// TODO: generate functions

			output.WriteOp(MC.OP_START_INIT, 0, 0);

			// initialize globals
			
			Heap.WriteHeap(output);
			//currentContext = sem.contexts[0]; // = global
			//NodeIterator it = new NodeIterator(tree.root);
			//GenerateCodeBlock(it.Copy());

			output.WriteOp(MC.OP_END_INIT, 0, 0);
			
			// TODO: write special type data, eg. 'map'
		}

		public void PrintCurrentContext()
		{
			MS.Print("currentContext: ");
			for (int i = 0; i < currentContextData.data.Length; i++)
			{
				MS.Print("    " + i + ":    " + currentContextData.data[i]);
			}
		}

		public void PrintDetails()
		{
			MS.Print(MS.Title("DETAILS"));
			MS.Print("stack top:    " + stackTop);
			//MS.Print("instruction pointer: " + instructionPointer);

			MS.Print("\nSTACK");
			MC.PrintBytecode(stack.Data(), stackTop, -1, false);
		}

		public void PrintCode()
		{
			if (input is MSInputArray a)
			{
				MS.Print(MS.Title("BYTECODE CONTENTS"));
				MS.Print("index     code/data (32 bits)");
				MC.PrintBytecode(a.Data(), a.Data().Length, -1, true);
			}
		}
	}
}
