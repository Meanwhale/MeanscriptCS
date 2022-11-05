namespace Meanscript
{

	public class Generator : MC
	{
		Context currentContext;
		readonly TokenTree tree;
		readonly Semantics sem;
		readonly Common common;
		readonly ByteCode bc;

		public Generator(TokenTree _tree, Semantics _sem, Common _common)
		{
			sem = _sem;
			common = _common;
			bc = new ByteCode(common);
			tree = _tree;
			currentContext = null;
		}

		//

		public bool InGlobal()
		{
			return currentContext == sem.contexts[0];
		}

		public static ByteCode Generate(TokenTree _tree, Semantics _sem, Common _common)
		{
			Generator gen = new Generator(_tree, _sem, _common);
			gen.Generate();
			return gen.bc;
		}

		public ByteCode Generate()
		{

			MS.Verbose("------------------------ GENERATE GLOBAL CODE");

			currentContext = sem.contexts[0];

			// start
			bc.AddInstructionWithData(OP_START_INIT, 1, BYTECODE_EXECUTABLE, tree.textCount);

			// add texts (0 = empty)

			for (int i = 1; i < tree.textCount; i++)
			{
				MSText t = sem.GetText(i);
				MS.Assertion(t != null, MC.EC_INTERNAL, "tree texts not containing text ID: " + i);
				bc.codeTop = AddTextInstruction(t, OP_ADD_TEXT, bc.code, bc.codeTop);
			}

			// define structure types
			sem.WriteStructDefs(bc);

			// introduce functions
			for (int i = 0; i < sem.maxContexts; i++)
			{
				if (sem.contexts[i] != null)
				{
					sem.contexts[i].tagAddress = bc.codeTop;
					bc.AddInstruction(OP_FUNCTION, 5, 0);
					bc.AddWord(sem.contexts[i].functionID);
					bc.AddWord(-1); // add start address later
					bc.AddWord(-1); // add struct size later (temp. variables may be added)
					bc.AddWord(sem.contexts[i].variables.argsSize);
					bc.AddWord(-1); // add end address later...
									// ...where the 'go back' instruction is, or in the future
									// some local un-initialization or something.
									// 'return' takes you there.
				}
			}
			bc.AddInstruction(OP_END_INIT, 0, 0);

			currentContext = sem.contexts[0]; // = global
			currentContext.codeStartAddress = bc.codeTop;

			NodeIterator it = new NodeIterator(tree.root);

			GenerateCodeBlock(it.Copy());

			currentContext.codeEndAddress = bc.codeTop;
			bc.AddInstruction(OP_GO_BACK, 0, 0); // end of global code

			for (int i = 1; i < sem.maxContexts; i++)
			{
				if (sem.contexts[i] != null)
				{
					MS.Verbose("------------------------ GENERATE FUNCTION CODE");
					currentContext = sem.contexts[i];
					NodeIterator iter = new NodeIterator(currentContext.codeNode);
					GenerateFunctionCode(iter.Copy());
				}
			}
			MS.Verbose("------------------------ write code addresses");

			for (int i = 0; i < sem.maxContexts; i++)
			{
				if (sem.contexts[i] != null)
				{
					bc.code[(sem.contexts[i].tagAddress) + 2] = sem.contexts[i].codeStartAddress;
					bc.code[(sem.contexts[i].tagAddress) + 3] = sem.contexts[i].variables.structSize;
					bc.code[(sem.contexts[i].tagAddress) + 5] = sem.contexts[i].codeEndAddress;
				}
			}
			MS.Verbose("------------------------ END GENERATION");
			return bc;
		}

		public void GenerateFunctionCode(NodeIterator it)
		{
			it.ToChild();
			currentContext.codeStartAddress = bc.codeTop;
			GenerateCodeBlock(it);
			currentContext.codeEndAddress = bc.codeTop;
			bc.AddInstruction(OP_GO_BACK, 0, 0);
		}

		public void GenerateCodeBlock(NodeIterator it)
		{
			while (true)
			{
				if (it.Type() == NT_EXPR)
				{
					if (!it.HasChild())
					{
						MS.Verbose("<EMPTY EXPR>");
					}
					else
					{
						// make a new iterator for child
						it.ToChild();
						GenerateExpression(it.Copy());
						it.ToParent();
					}

					if (!it.HasNext()) return;

					it.ToNext();
				}
				else
				{
					MS.SyntaxAssertion(false, it, "expression expected");
				}
			}
		}


		public void GenerateExpression(NodeIterator it)
		{
			MS.Verbose("------------ read expr ------------");
			if (MS._verboseOn) it.PrintTree(false);

			if (it.Type() == NT_NAME_TOKEN)
			{
				Context context = sem.FindContext(it.Data());

				if (context != null)
				{
					GenerateFunctionCall(it, context);
				}
				else if ((common.callbackIDs.ContainsKey(it.Data())))
				{
					GenerateCallbackCall(it);
				}
				else if (currentContext.variables.HasMember(it.Data()))
				{
					GenerateAssignment(it);
				}
				else if (sem.HasType(it.Data()))
				{
					MS.Verbose("Initialize a variable");
					it.ToNext();

					if (it.Type() == NT_SQUARE_BRACKETS)
					{
						// eg. "int [] numbers" or "person [5] team"
						it.ToNext();
					}

					MS.SyntaxAssertion(currentContext.variables.HasMember(it.Data()), it, "unknown variable: " + it.Data());
					if (it.HasNext()) GenerateAssignment(it);
				}
				else if ((it.Data()).Match(keywords[KEYWORD_RETURN_ID]))
				{
					MS.Verbose("Generate a return call");
					MS.SyntaxAssertion(it.HasNext(), it, "'return' is missing a value"); // TODO: return from a void context
					it.ToNext();
					MS.SyntaxAssertion(!it.HasNext(), it, "'return' can take only one value");
					MS.SyntaxAssertion(currentContext.returnType >= 0, it, "can't return");

					// TODO: return value could be an array, a reference, etc.
					SingleArgumentPush(currentContext.returnType, it, -1);

					bc.AddInstruction(OP_POP_STACK_TO_REG, 1, currentContext.returnType);
					bc.AddWord(sem.GetType(currentContext.returnType, it).structSize);
					bc.AddInstruction(OP_GO_END, 0, 0);
				}
				else if ((it.Data()).Match(keywords[KEYWORD_STRUCT_ID]))
				{
					MS.Verbose("Skip a struct definition");
				}
				else if ((it.Data()).Match(keywords[KEYWORD_FUNC_ID]))
				{
					MS.Verbose("Skip a function definition for now");
				}
				else
				{
					MS.SyntaxAssertion(false, it, "unknown word: " + it.Data());
				}
			}
			else
			{
				MS.SyntaxAssertion(false, it, "unexpected token");
			}
		}

		public void GenerateFunctionCall(NodeIterator it, Context funcContext)
		{
			MS.Verbose("Generate a function call");

			bc.AddInstruction(OP_SAVE_BASE, 0, 0);

			if (funcContext.numArgs != 0)
			{
				MS.SyntaxAssertion(it.HasNext(), it, "function arguments expected");
				it.ToNext();
				CallArgumentPush(it.Copy(), funcContext.variables, funcContext.numArgs);
			}
			bc.AddInstructionWithData(OP_FUNCTION_CALL, 1, 0, funcContext.functionID);
			bc.AddInstruction(OP_LOAD_BASE, 0, 0);
		}

		public MCallback GenerateCallbackCall(NodeIterator it)
		{
			int callbackID = common.callbackIDs[it.Data()];
			MCallback callback = common.callbacks[callbackID];
			MS.Verbose("Callback call, id " + callbackID);
			if (callback.argStruct.numMembers > 0)
			{
				it.ToNext();
				CallArgumentPush(it.Copy(), callback.argStruct, callback.argStruct.numMembers);
			}
			bc.AddInstructionWithData(OP_CALLBACK_CALL, 1, 0, callbackID);
			return callback;
		}

		public void GenerateAssignment(NodeIterator it)
		{
			// e.g. "int a:5" or "a:6"

			MS.Verbose("Add value assinging instructions");

			// get assignment target 

			VarGen target = ResolveMember(it);

			it.ToNext();
			MS.Assertion(it.Type() == NT_ASSIGNMENT, MC.EC_INTERNAL, "assignment struct expected");

			if (target.IsArray())
			{
				// assign array
				MS.SyntaxAssertion(target.arraySize == it.NumChildren(), it, "wrong number of arguments in array assignment");
				MS.SyntaxAssertion(!target.isReference, it, "array reference can't be assigned");

				// assign children i.e. array items

				it.ToChild();

				int arrayDataSize = ArrayPush(it, target.type, target.arraySize);

				bc.AddInstruction(InGlobal() ? OP_POP_STACK_TO_GLOBAL : OP_POP_STACK_TO_LOCAL, 2, MS_TYPE_VOID);
				bc.AddWord(arrayDataSize);
				bc.AddWord(target.address);

				it.ToParent();

				return;
			}

			StructDef typeSD = sem.GetType(target.type, it);

			// get value for assignment target

			it.ToChild();
			if (it.HasNext())
			{
				// list of arguments to assign
				ArgumentStructPush(it.Copy(), typeSD, typeSD.numMembers, true);
			}
			else
			{
				NodeIterator cp = new NodeIterator(it);
				SingleArgumentPush(target.type, cp, target.charCount); // last arg. > 0 if the type is chars
			}

			// WRITE values. This works like a callback call.
			// Actually here we could call overridden assignment callback for the type.

			// local or global?
			if (target.isReference)
			{
				bc.AddInstruction(InGlobal() ? OP_POP_STACK_TO_GLOBAL_REF : OP_POP_STACK_TO_LOCAL_REF, 2, MS_TYPE_VOID);
			}
			else
			{
				bc.AddInstruction(InGlobal() ? OP_POP_STACK_TO_GLOBAL : OP_POP_STACK_TO_LOCAL, 2, MS_TYPE_VOID);
			}

			if (target.type == MS_GEN_TYPE_CHARS)
			{
				bc.AddWord(target.charCount / 4 * 2);
			}
			else
			{
				MS.Assertion(sem.GetType(target.type, it).structSize > 0, MC.EC_INTERNAL, "...");
				bc.AddWord(sem.GetType(target.type, it).structSize);
			}

			bc.AddWord(target.address);
		}

		public int ArrayPush(NodeIterator it, int itemType, int arraySize)
		{
			StructDef itemSD = sem.GetType(itemType);
			int itemSize = itemSD.structSize;

			for (int i = 0; i < arraySize; i++)
			{
				it.ToChild();
				NodeIterator cp = new NodeIterator(it);
				SingleArgumentPush(itemType, cp, -1);
				it.ToParent();
				if (it.HasNext()) it.ToNext();
			}
			return arraySize * itemSize;
		}

		public void SquareBracketArgumentPush(NodeIterator it, StructDef sd, int numArgs)
		{
			MS.Verbose("Assign struct values in square brackets");

			int argIndex = 0;
			it.ToChild();
			MS.Assertion(it.Type() == NT_EXPR, MC.EC_INTERNAL, "expression expected");
			MS.Assertion(it.HasChild(), MC.EC_INTERNAL, "argument expected");

			do
			{
				it.ToChild();
				MS.SyntaxAssertion(argIndex < numArgs, it, "wrong number of arguments, expected " + numArgs);
				int memberType = InstrValueTypeID(sd.GetMemberTagByIndex(argIndex));
				int numItems = -1;
				if (memberType == MS_GEN_TYPE_ARRAY)
				{
					numItems = sd.GetMemberArrayItemCountOrNegative(argIndex);
				}
				if (memberType == MS_GEN_TYPE_CHARS)
				{
					numItems = sd.GetMemberCharCount(argIndex);
				}
				SingleArgumentPush(memberType, it, numItems);
				it.ToParent();
				argIndex++;
			}
			while (it.ToNextOrFalse());

			it.ToParent();

			MS.SyntaxAssertion(!(it.HasNext()) && argIndex == numArgs, it, "wrong number of arguments");
		}

		public void CallArgumentPush(NodeIterator it, StructDef sd, int numArgs)
		{
			if ((it.Type() == NT_PARENTHESIS && !it.HasNext()))
			{
				// F2 (a1, a2)

				it.ToChild();
				ArgumentStructPush(it, sd, numArgs, true);
			}
			else
			{
				// F1 a1
				// F2 a1 a2
				// F2 (F3 x) a2

				ArgumentStructPush(it, sd, numArgs, false);
			}
		}

		public void ArgumentStructPush(NodeIterator it, StructDef sd, int numArgs, bool commaSeparated)
		{
			MS.Verbose("Assign struct argument");

			// HANDLE BOTH CASES:
			// 1)		func arg1 arg2
			// 2)		func (arg1, arg2)

			int argIndex = 0;
			do
			{
				if (!commaSeparated)
				{
					MS.SyntaxAssertion(!IsFunctionOrCallback(it.Data()), it, "function arguments must be in brackets or comma-separated");
				}
				else
				{
					it.ToChild(); // comma-separated are expressions
				}

				MS.SyntaxAssertion(sd.IndexInRange(argIndex), it, "too many arguments");
				int memberTag = sd.GetMemberTagByIndex(argIndex);
				int arrayItemCount = sd.GetMemberArrayItemCountOrNegative(argIndex);
				SingleArgumentPush(InstrValueTypeID(memberTag), it, arrayItemCount);

				if (commaSeparated)
				{
					it.ToParent();
				}

				argIndex++;
			}
			while (it.ToNextOrFalse());

			MS.SyntaxAssertion(!(it.HasNext()) && argIndex == numArgs, it, "wrong number of arguments");
		}

		public bool IsFunctionOrCallback(MSText name)
		{
			Context context = sem.FindContext(name);
			if (context == null) return ((common.callbackIDs.ContainsKey(name)));
			return true;
		}


		public VarGen ResolveMember(NodeIterator it)
		{
			// read a variable from a chain of nodes and return its address, type, etc.
			// as a VarGen. For example "x", "arr[foo].bar", "arr[f(a)].bar".

			bool isReference = false;
			int auxAddress = -1;
			int lastOffsetCodeIndex = -1;
			int arrayItemCount = -1;
			int charCount = -1;

			StructDef currentStruct = currentContext.variables;
			int memberType = (int)(currentStruct.GetMemberTagByName(it.Data()) & VALUE_TYPE_MASK);
			int size = currentStruct.GetMemberSizeByName(it.Data());
			int srcAddress = currentStruct.GetMemberAddressByName(it.Data());

			if (memberType == MS_GEN_TYPE_ARRAY)
			{
				arrayItemCount = currentStruct.GetMemberArrayItemCount(it.Data());
			}
			else if (memberType == MS_GEN_TYPE_CHARS)
			{
				charCount = currentStruct.GetCharCount(it.Data());
			}
			else
			{
				arrayItemCount = -1;
			}

			while (true)
			{
				MS.Verbose("RESOLVER size:" + size + " mtype:" + memberType + " addr:" + srcAddress + " arrC:" + arrayItemCount + " charCount:" + charCount + " ref:" + isReference);

				if (it.HasNext() && it.NextType() == NT_DOT)
				{
					// e.g. "age" in "group.person.age: 41"	
					it.ToNext();
					MS.SyntaxAssertion(it.HasNext() && it.NextType() == NT_NAME_TOKEN, it, "name expected after a dot");
					it.ToNext();

					// StructDef memberType = sem.getType((int)(memberTag & VALUE_TYPE_MASK), it);
					// memberTag = memberType.getMemberTagByName(it.data());

					currentStruct = sem.GetType(memberType, it);

					MS.Verbose("    DOT: " + it.Data());

					if (isReference)
					{
						bc.code[lastOffsetCodeIndex] += currentStruct.GetMemberAddressByName(it.Data());
					}
					else
					{
						size = currentStruct.GetMemberSizeByName(it.Data());
						srcAddress += currentStruct.GetMemberAddressByName(it.Data()); // offset for the value
					}
					memberType = InstrValueTypeID(currentStruct.GetMemberTagByName(it.Data()));

					arrayItemCount = -1;
					charCount = -1;
					if (memberType == MS_GEN_TYPE_ARRAY)
					{
						arrayItemCount = currentStruct.GetMemberArrayItemCount(it.Data());
					}
					else if (memberType == MS_GEN_TYPE_CHARS)
					{
						charCount = currentStruct.GetCharCount(it.Data());
					}
				}
				else if (it.HasNext() && it.NextType() == NT_SQUARE_BRACKETS)
				{
					MS.Verbose("    SQ. BRACKETS");

					// e.g. "numbers[4]"

					MS.SyntaxAssertion(memberType == MS_GEN_TYPE_ARRAY, it, "array expected");

					arrayItemCount = currentStruct.GetMemberArrayItemCount(it.Data());
					int arrayItemTypeID = currentStruct.GetMemberArrayItemType(it.Data());
					it.ToNext();

					// array index
					it.ToChild();
					MS.SyntaxAssertion(!it.HasNext(), it, "array index expected");
					it.ToChild();

					// get array item type
					StructDef arrayItemType = sem.GetType(arrayItemTypeID, it);
					int itemSize = arrayItemType.structSize;

					if (it.Type() == NT_NUMBER_TOKEN)
					{
						MS.SyntaxAssertion(!it.HasNext(), it, "array index expected");

						// array index (number) expected");
						int arrayIndex = MS.ParseInt(it.Data().GetString());
						// mul. size * index, and plus one as the array size is at [0]
						MS.SyntaxAssertion(arrayIndex >= 0 && arrayIndex < arrayItemCount, it, "index out of range: " + arrayIndex + " of " + arrayItemCount);
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
						SingleArgumentPush(MS_TYPE_INT, cp, -1);

						if (auxAddress < 0)
						{
							// create a auxiliar variable
							auxAddress = currentStruct.AddMember(-1, MS_TYPE_INT, 1);
						}

						// write index value to variable

						bc.AddInstruction(OP_MULTIPLY_GLOBAL_ARRAY_INDEX, 4, MS_TYPE_INT);
						bc.AddWord(auxAddress);             // address to array index
						bc.AddWord(itemSize);               // size of one array item

						lastOffsetCodeIndex = bc.codeTop;   // save the address to add offset later if needed

						if (isReference)
						{
							// tell MeanMachine that we want to add to the previous address as
							// this is not the first variable index of the chain,
							// e.g. "team[foo].position[bar]"
							bc.AddWord(-1);
						}
						else
						{
							bc.AddWord(srcAddress);         // address of the array data (size first)
						}
						bc.AddWord(arrayItemCount);         // save item count to SYNTAX for array out-of-bounds

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

					it.ToParent();
					it.ToParent();
				}
				else break;
			}

			if (isReference) { MS.Assertion(size == 1, MC.EC_INTERNAL, ""); }

			// arrays returns arrayItemCount > 0 and itemType as memberType
			MS.Assertion(memberType != MS_GEN_TYPE_ARRAY, MC.EC_INTERNAL, "resolveMember can't return MS_GEN_TYPE_ARRAY");

			MS.Verbose("RES. OUT size:" + size + " mtype:" + memberType + " addr:" + srcAddress + " arrC:" + arrayItemCount + " charCount:" + charCount + " ref:" + isReference);

			return new VarGen(size, memberType, srcAddress, arrayItemCount, charCount, isReference);
		}

		public void SingleArgumentPush(int targetType, NodeIterator it, int arrayItemCount)
		{
			MS.Verbose("Assign an argument [" + it.Data() + "]");

			MS.Assertion(targetType < MAX_TYPES, MC.EC_INTERNAL, "invalid type");

			if (it.Type() == NT_EXPR)
			{
				MS.SyntaxAssertion(!it.HasNext(), it, "argument syntax error");
				it.ToChild();
				NodeIterator cp = new NodeIterator(it);
				SingleArgumentPush(targetType, cp, arrayItemCount);
				it.ToParent();
				return;
			}

			if (it.Type() == NT_HEX_TOKEN)
			{
				if (targetType == MS_TYPE_INT)
				{
					long number = ParseHex((it.Data()).GetString(), 8);
					bc.AddInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_INT, Int64lowBits(number));
					return;
				}
				else if (targetType == MS_TYPE_INT64)
				{
					long number = ParseHex((it.Data()).GetString(), 16);
					bc.AddInstruction(OP_PUSH_IMMEDIATE, 2, MS_TYPE_INT64);
					bc.AddWord(Int64highBits(number));
					bc.AddWord(Int64lowBits(number));
					return;
				}
				else
				{
					MS.SyntaxAssertion(false, it, "number error");
				}
			}
			else if (it.Type() == NT_NUMBER_TOKEN)
			{
				if (targetType == MS_TYPE_INT)
				{
					int number = MS.ParseInt((it.Data()).GetString());
					bc.AddInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_INT, number);
					return;
				}
				else if (targetType == MS_TYPE_INT64)
				{
					long number = MS.ParseInt64((it.Data()).GetString());
					bc.AddInstruction(OP_PUSH_IMMEDIATE, 2, MS_TYPE_INT64);
					bc.AddWord(Int64highBits(number));
					bc.AddWord(Int64lowBits(number));
					return;
				}
				else if (targetType == MS_TYPE_FLOAT)
				{
					float f = MS.ParseFloat32(it.Data().GetString());
					int floatToInt = MS.FloatToIntFormat(f);
					bc.AddInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_FLOAT, floatToInt);
					return;
				}
				else if (targetType == MS_TYPE_FLOAT64)
				{
					double f = MS.ParseFloat64((it.Data()).GetString());
					long number = MS.Float64ToInt64Format(f);
					bc.AddInstruction(OP_PUSH_IMMEDIATE, 2, MS_TYPE_FLOAT64);
					bc.AddWord(Int64highBits(number));
					bc.AddWord(Int64lowBits(number));
					return;
				}
				else
				{
					MS.SyntaxAssertion(false, it, "number error");
				}
			}
			else if (it.Type() == NT_TEXT)
			{
				int textID = tree.GetTextID(it.Data());
				MS.Assertion(textID >= 0, MC.EC_INTERNAL, "text not found");
				if (targetType == MS_TYPE_TEXT)
				{
					// assign text id
					bc.AddInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_TEXT, textID);
				}
				else
				{
					// copy chars
					int maxChars = arrayItemCount; // eg. 7 if "chars[7] x"
												   // StructDef sd = sem.typeStructDefs[targetType];
					MS.SyntaxAssertion(targetType == MS_GEN_TYPE_CHARS, it, "chars type expected");
					bc.AddInstructionWithData(OP_PUSH_CHARS, 3, MS_TYPE_TEXT, textID);
					bc.AddWord(maxChars);
					bc.AddWord((maxChars / 4) + 2 + 1); // characters + size
				}
				return;
			}
			else if (it.Type() == NT_NAME_TOKEN)
			{
				Context functionContext = sem.FindContext(it.Data());
				if (functionContext != null)
				{
					// PUSH A FUNCTION ARGUMENT

					GenerateFunctionCall(it.Copy(), functionContext);
					StructDef returnData = sem.GetType(functionContext.returnType, it);
					MS.SyntaxAssertion(targetType == returnData.typeID, it, "type mismatch");
					bc.AddInstructionWithData(OP_PUSH_REG_TO_STACK, 1, MS_TYPE_VOID, returnData.structSize);
					return;
				}
				else if ((common.callbackIDs.ContainsKey(it.Data())))
				{

					// PUSH A CALLBACK ARGUMENT

					MCallback callback = GenerateCallbackCall(it.Copy());
					StructDef returnData = sem.GetType(callback.returnType, it);
					MS.SyntaxAssertion(targetType == returnData.typeID, it, "type mismatch");
					bc.AddInstructionWithData(OP_PUSH_REG_TO_STACK, 1, MS_TYPE_VOID, returnData.structSize);
					return;
				}
				else
				{
					// PUSH A VARIABLE ARGUMENT

					VarGen vg = ResolveMember(it);

					// write the address or its reference from where to push

					if (vg.isReference)
					{
						bc.AddInstruction(InGlobal() ? OP_PUSH_GLOBAL_REF : OP_PUSH_LOCAL_REF, 2, MS_TYPE_INT);
					}
					else
					{
						bc.AddInstruction(InGlobal() ? OP_PUSH_GLOBAL : OP_PUSH_LOCAL, 2, MS_TYPE_INT);
					}

					bc.AddWord(vg.address);
					bc.AddWord(vg.size);

					MS.SyntaxAssertion(targetType == vg.type, it, "type mismatch");

					return;
				}
			}
			else if (it.Type() == NT_PARENTHESIS)
			{
				it.ToChild();
				NodeIterator cp = new NodeIterator(it);
				SingleArgumentPush(targetType, cp, arrayItemCount);
				it.ToParent();
				return;
			}
			else if (it.Type() == NT_REFERENCE_TOKEN)
			{
				// TODO: SYNTAX type
				int memberTag = currentContext.variables.GetMemberTagByName(it.Data());
				int address = currentContext.variables.GetMemberAddressByName(it.Data());
				int memberType = (int)(memberTag & VALUE_TYPE_MASK);
				bc.AddInstructionWithData(OP_PUSH_IMMEDIATE, 1, memberType, address);
				return;
			}
			else if (it.Type() == NT_CODE_BLOCK)
			{
				bc.AddInstructionWithData(OP_JUMP, 1, MS_TYPE_CODE_ADDRESS, -1); // -1 to be replaced after code block is done
				int jumpAddressPosition = bc.codeTop - 1;
				int blockAddress = bc.codeTop;

				bc.AddInstruction(OP_NOOP, 0, 0); // no-op in case that the block is empty

				// generate code block

				it.ToChild();
				MS.Assertion(it.Type() == NT_EXPR, MC.EC_INTERNAL, "expression expected");
				MS.Verbose(" - - - - start generating code block");
				GenerateCodeBlock(it);
				bc.AddInstruction(OP_GO_BACK, 0, 0);
				MS.Verbose(" - - - - end generating code block");
				it.ToParent();

				bc.code[jumpAddressPosition] = bc.codeTop;
				bc.AddInstruction(OP_NOOP, 0, 0);

				bc.AddInstructionWithData(OP_PUSH_IMMEDIATE, 1, MS_TYPE_CODE_ADDRESS, blockAddress); // push the argument: block address
				return;
			}
			else if (it.Type() == NT_SQUARE_BRACKETS)
			{
				if (targetType == MS_GEN_TYPE_ARRAY)
				{
					// push a list of array values

					it.ToChild();
					ArrayPush(it.Copy(), targetType, arrayItemCount);
					it.ToParent();
					return;
				}
				else
				{
					// push array values

					StructDef sd = sem.typeStructDefs[targetType];
					MS.SyntaxAssertion(sd != null, it, "generator: unknown type: " + targetType);
					SquareBracketArgumentPush(it.Copy(), sd, sd.numMembers);
					return;
				}
			}
			else
			{
				MS.SyntaxAssertion(false, it, "argument error");
			}
		}


	}
}
