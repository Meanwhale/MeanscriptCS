using System;

namespace Meanscript
{
	public class Generator
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

		private bool InGlobal()
		{
			return currentContext == sem.contexts[0];
		}

		public static ByteCode Generate(TokenTree _tree, Semantics _sem, Common _common)
		{
			Generator gen = new Generator(_tree, _sem, _common);
			gen.Generate();
			return gen.bc;
		}

		private ByteCode Generate()
		{

			MS.Verbose("------------------------ GENERATE GLOBAL CODE");

			currentContext = sem.contexts[0];

			// start
			bc.AddInstructionWithData(MC.OP_START_INIT, 1, MC.BYTECODE_EXECUTABLE, tree.textCount);

			// add texts (0 = empty)

			for (int i = 1; i < tree.textCount; i++)
			{
				MSText t = sem.GetText(i);
				MS.Assertion(t != null, MC.EC_INTERNAL, "tree texts not containing text ID: " + i);
				bc.codeTop = MC.AddTextInstruction(t, MC.OP_ADD_TEXT, bc.code, bc.codeTop);
			}

			// define structure types
			sem.WriteStructDefs(bc);

			// introduce functions
			for (int i = 0; i < sem.maxContexts; i++)
			{
				if (sem.contexts[i] != null)
				{
					sem.contexts[i].tagAddress = bc.codeTop;
					bc.AddInstruction(MC.OP_FUNCTION, 5, 0);
					bc.AddWord(sem.contexts[i].functionID);
					bc.AddWord(-1); // add start address later
					bc.AddWord(-1); // add struct size later (temp. variables may be added)
					bc.AddWord(sem.contexts[i].variables.ArgsSize); // args part of stack
					bc.AddWord(-1); // add end address later...
									// ...where the 'go back' instruction is, or in the future
									// some local un-initialization or something.
									// 'return' takes you there.
				}
			}
			bc.AddInstruction(MC.OP_END_INIT, 0, 0);

			currentContext = sem.contexts[0]; // = global
			currentContext.codeStartAddress = bc.codeTop;

			NodeIterator it = new NodeIterator(tree.root);

			GenerateCodeBlock(it.Copy());

			currentContext.codeEndAddress = bc.codeTop;
			bc.AddInstruction(MC.OP_GO_BACK, 0, 0); // end of global code

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
					bc.code[(sem.contexts[i].tagAddress) + 3] = sem.contexts[i].variables.StructSize();
					bc.code[(sem.contexts[i].tagAddress) + 5] = sem.contexts[i].codeEndAddress;
				}
			}
			MS.Verbose("------------------------ END GENERATION");
			return bc;
		}

		private void GenerateFunctionCode(NodeIterator it)
		{
			it.ToChild();
			currentContext.codeStartAddress = bc.codeTop;
			GenerateCodeBlock(it);
			currentContext.codeEndAddress = bc.codeTop;
			bc.AddInstruction(MC.OP_GO_BACK, 0, 0);
		}

		private void GenerateCodeBlock(NodeIterator it)
		{
			while (true)
			{
				var exprType = it.Type();
				if (exprType == NodeType.EXPR)
				{
					if (!it.HasChild())
					{
						MS.Verbose("---- EMPTY EXPR ----");
					}
					else
					{
						// make a new iterator for child
						GenerateExpression(it.Copy());
					}

				}
				else if (exprType == NodeType.EXPR_INIT)
				{
					MS.Verbose("skip variable init");
				}
				else if (exprType == NodeType.EXPR_STRUCT)
				{
					MS.Verbose("skip struct");
				}
				else if (exprType == NodeType.EXPR_ASSIGN)
				{
					MS.Verbose("EXPR_ASSIGN");
					var cp = it.Copy();
					cp.ToChild();
					GenerateAssign(cp);
				}
				else if (exprType == NodeType.EXPR_INIT_AND_ASSIGN)
				{
					MS.Verbose("EXPR_INIT_AND_ASSIGN");
					GenerateInitAndAssign(it.Copy());
				}
				else if (exprType != NodeType.EXPR_STRUCT)
				{
					MS.SyntaxAssertion(false, it, "expression expected");
					return;
				}
				else
				{
					MS.SyntaxAssertion(false, it, "unhandled token");
				}
				if (!it.HasNext()) return;
				it.ToNext();
			}
		}

		private void GenerateInitAndAssign(NodeIterator it)
		{
			MS.Verbose("------------ GenerateInitAndAssign ------------");
			if (MS._verboseOn) it.PrintTree(false);
			// eg. "int a: 5"

			//		[<EXPR>]
			//			[int]
			//			[a]
			//			[<ASSIGNMENT>]
			//				[<EXPR>]
			//			      [5]
			//		[<EXPR>] ...

			MS.Assertion(it.Type() == NodeType.EXPR_INIT_AND_ASSIGN);
			it.ToChild();
			var memberType = sem.GetDataType(it.Data()); // eg. "int"
			it.ToNext();

			GenerateAssign(it);
		}

		private void GenerateAssign(NodeIterator it)
		{
			MS.Verbose("------------ GenerateAssign ------------");
			if (MS._verboseOn) it.PrintTree(false);
			// eg. "a: 5"

			//var member = sem.currentContext.variables.GetMember(it.Data()); // eg. "a"
			if (it.Type() == NodeType.SQUARE_BRACKETS && it.GetParent().type == NodeType.EXPR_INIT_AND_ASSIGN)
			{
				it.ToNext(); // generic init & assign
			}
			var assignTarget = ResolveAndPushVariableAddress(it);
			it.ToNext();
			MS.Assertion(it.Type() == NodeType.ASSIGNMENT_BLOCK, MC.EC_INTERNAL, "assignment struct expected");
			it.ToChild(); // expression that contains value(s) to assign, eg. "5"
			MS.Assertion(it.Type() == NodeType.EXPR);
			it.ToChild();
			
			// push values and make conversions if available
			PushAssignValues(it, assignTarget);

			bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.MS_TYPE_INT, assignTarget.Def.SizeOf());

			// stack: ... [target address][           data           ][size]

			bc.AddInstruction(InGlobal() ? MC.OP_POP_STACK_TO_GLOBAL : MC.OP_POP_STACK_TO_LOCAL, 0, MC.MS_TYPE_VOID);
		}

		private void GenerateExpression(NodeIterator it)
		{
			MS.Assertion(it.Type() == NodeType.EXPR);
			it.ToChild();
			MS.Verbose("------------ GenerateExpression ------------");
			if (MS._verboseOn) it.PrintTree(false);

			if (it.Type() == NodeType.NAME_TOKEN)
			{
				// get list of arguments and find a call that match the list.

				// TODO: check if it's "return"

				var args = PushArgs(it);

				// try to find a callback

				var callback = common.FindCallback(args);
				if (callback != null)
				{
					MS.Verbose("callback found: ");
					callback.Print(MS.printOut);
					bc.AddInstruction(MC.OP_CALLBACK_CALL, 0, callback.ID);
					return;
				}
			}
			else
			{
				MS.SyntaxAssertion(false, it, "unexpected token");
			}
		}
		
		private void PushAssignValues(NodeIterator it, ArgType trg)
		{
			// Assume "it" is on expression's first token, value to assign.
			if (trg.Def is PrimitiveType)
			{
				ResolveAndPushArgument(it, trg.Def);
			}
			else if (trg.Def is GenericCharsType)
			{
				// write size and chars
				ResolveAndPushArgument(it, trg.Def);
			}
			else
			{
				MS.Assertion(false);
			}
		}

		private MList<ArgType> PushArgs(NodeIterator it)
		{	
			// Push expression arguments and return type list.
			// Assume "it" is on expression's first token.

			var args = new MList<ArgType>();
			while (true)
			{
				var a = ResolveAndPushArgument(it);
				if (a == null) break;
				MS.Verbose("ARG type: " + a);
				args.Add(a);

				if (!it.HasNext()) break;
				it.ToNext();
			}
			return args;
		}

		private ArgType ResolveAndPushArgument(NodeIterator it, TypeDef trg = null)
		{
			// move the iterator forward inside the expression

			if (it.node.type == NodeType.NAME_TOKEN)
			{
				var typeArg = sem.GetType(it.Data());
				if (typeArg != null)
				{
					if (typeArg is CallNameType cn)
					{
						return ArgType.Void(cn);
					}
				}
				else
				{
					return ResolveAndPushVariableData(it);
				}
			}
			else
			{
				return SingleArgumentPush(it, trg);
			}
			MS.SyntaxAssertion(false, it, "unexpected argument");
			return null;
		}
		private ArgType ResolveAndPushVariableData(NodeIterator it)
		{
			var arg = ResolveAndPushVariableAddress(it);
			
			bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.MS_TYPE_INT, arg.Def.SizeOf());

			// OP_PUSH_GLOBAL/LOCAL gets address and size from stack and push to the stack

			bc.AddInstruction(InGlobal() ? MC.OP_PUSH_GLOBAL : MC.OP_PUSH_LOCAL, 0, MC.MS_TYPE_INT);

			return arg;
		}

		private ArgType ResolveAndPushVariableAddress(NodeIterator it)
		{	
			var member = sem.currentContext.variables.GetMember(it.Data());
			MS.SyntaxAssertion(member != null, it, "unknown: " + it.Data()); 
			var varType = member.Type;
			int address = member.Address;
			bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.MS_TYPE_INT, address);

			while(true)
			{
				if (!it.HasNext()) break;
				if (it.NextType() == NodeType.DOT)
				{
					// eg. "vec.x"
					it.ToNext();
					MS.SyntaxAssertion(it.HasNext(), it, "member after dot expected");
					MS.SyntaxAssertion(varType is StructDefType, it, "struct before dot expected: " + varType);
					it.ToNext();
					member = ((StructDefType)varType).SD.GetMember(it.Data());
					MS.SyntaxAssertion(member != null, it, "unknown member: " + it.Data());
					bc.AddInstructionWithData(MC.OP_ADD_TOP, 1, MC.MS_TYPE_INT, member.Address);
					varType = member.Type;
					continue;
				}
				else if (it.NextType() == NodeType.SQUARE_BRACKETS)
				{
					// push argument in square brackets and call getter
					// eg. "a[2]"
					//	[<EXPR>]
					//		[a]
					//		[<SQUARE_BRACKETS>]
					//			[<EXPR>]
					//				[2]
					MS.SyntaxAssertion(member.Type is GenericArrayType, it, "array expected");
					var arrayType = (GenericArrayType)member.Type;
					it.ToNext();
					var cp = it.Copy();
					cp.ToChild();
					MS.SyntaxAssertion(!cp.HasNext(), cp, "only one index expected");
					cp.ToChild();
					var args = PushArgs(cp);
					MS.SyntaxAssertion(arrayType.ValidIndex(args), it, "wrong array index");
					
					// now there should be the array address and the index in stack
					// so let's get the array item address by array's callback
					
					args.AddFirst(ArgType.Void(common.genericGetAtCallName));
					args.AddFirst(ArgType.Addr(arrayType));

					// agrs = arrayType, @getAt, index type
					// which Matched callback defined in GenericArrayType

					var callback = common.FindCallback(args);

					MS.SyntaxAssertion(callback != null, it, "array getter not found");

					MS.Verbose("array callback found: ");
					callback.Print(MS.printOut);
					bc.AddInstruction(MC.OP_CALLBACK_CALL, 0, callback.ID);

					// address is in register, so push it to stack

					bc.AddInstructionWithData(MC.OP_PUSH_REG_TO_STACK, 1, MC.MS_TYPE_VOID, 1);

					member = null;
					varType = arrayType.itemType;
					continue;
				}
				else if (it.NextType() == NodeType.PARENTHESIS)
				{
					// TODO: resolve the expression in parenthesis
					MS.Assertion(false);
				}
				break;
			}
			return ArgType.Data(varType);
		}

		
		
			/*
		private VarGen ResolveMember(NodeIterator it)
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

				if (it.HasNext() && it.NextType() == NodeType.DOT)
				{
					// e.g. "age" in "group.person.age: 41"	
					it.ToNext();
					MS.SyntaxAssertion(it.HasNext() && it.NextType() == NodeType.NAME_TOKEN, it, "name expected after a dot");
					it.ToNext();

					// StructDef memberType = sem.getType((int)(memberTag & VALUE_TYPE_MASK), it);
					// memberTag = memberType.getMemberTagByName(it.data());

					currentStruct = sem.GetStructDefType(memberType, it);

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
				else if (it.HasNext() && it.NextType() == NodeType.SQUARE_BRACKETS)
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
					StructDef arrayItemType = sem.GetStructDefType(arrayItemTypeID, it);
					int itemSize = arrayItemType.structSize;

					if (it.Type() == NodeType.NUMBER_TOKEN)
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

			return null;
		}

		private void GenerateAssignment(NodeIterator it)
		{
			// e.g. "int a:5" or "a:6"

			MS.Verbose("Add value assinging instructions");

			// get assignment target 

			VarGen target = ResolveMember(it);

			it.ToNext();
			MS.Assertion(it.Type() == NodeType.ASSIGNMENT, MC.EC_INTERNAL, "assignment struct expected");

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

			StructDef typeSD = sem.GetStructDefType(target.type, it);

			// get value for assignment target

			it.ToChild();
			if (it.HasNext())
			{
				// list of arguments to assign
				ArgumentStructPush(it.Copy(), typeSD, typeSD.NumMembers(), true);
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
				MS.Assertion(sem.GetType(target.type, it).Size() > 0, MC.EC_INTERNAL, "...");
				bc.AddWord(sem.GetType(target.type, it).Size());
			}

			bc.AddWord(target.address);
		}

		private int ArrayPush(NodeIterator it, int itemType, int arraySize)
		{
			StructDef itemSD = sem.GetStructDefType(itemType);
			int itemSize = itemSD.StructSize();

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

		private void SquareBracketArgumentPush(NodeIterator it, StructDef sd, int numArgs)
		{
			MS.Verbose("Assign struct values in square brackets");

			int argIndex = 0;
			it.ToChild();
			MS.Assertion(it.Type() == NodeType.EXPR, MC.EC_INTERNAL, "expression expected");
			MS.Assertion(it.HasChild(), MC.EC_INTERNAL, "argument expected");

			do
			{
				it.ToChild();
				MS.SyntaxAssertion(argIndex < numArgs, it, "wrong number of arguments, expected " + numArgs);
				int memberType = sd.GetMemberByIndex(argIndex).Type.ID;
				int numItems = -1;

				// LEGACY:
				//if (memberType == MS_GEN_TYPE_ARRAY)
				//{
				//	numItems = sd.GetMemberArrayItemCountOrNegative(argIndex);
				//}
				//if (memberType == MS_GEN_TYPE_CHARS)
				//{
				//	numItems = sd.GetMemberCharCount(argIndex);
				//}
				
				SingleArgumentPush(memberType, it, numItems);
				it.ToParent();
				argIndex++;
			}
			while (it.ToNextOrFalse());

			it.ToParent();

			MS.SyntaxAssertion(!(it.HasNext()) && argIndex == numArgs, it, "wrong number of arguments");
		}

		private void CallArgumentPush(NodeIterator it, StructDef sd, int numArgs)
		{
			if ((it.Type() == NodeType.PARENTHESIS && !it.HasNext()))
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

		private void ArgumentStructPush(NodeIterator it, StructDef sd, int numArgs, bool commaSeparated)
		{
			MS.Assertion(false);
			//MS.Verbose("Assign struct argument");
			//
			//// HANDLE BOTH CASES:
			//// 1)		func arg1 arg2
			//// 2)		func (arg1, arg2)
			//
			//int argIndex = 0;
			//do
			//{
			//	if (!commaSeparated)
			//	{
			//		MS.SyntaxAssertion(!IsFunctionOrCallback(it.Data()), it, "function arguments must be in brackets or comma-separated");
			//	}
			//	else
			//	{
			//		it.ToChild(); // comma-separated are expressions
			//	}
			//
			//	MS.SyntaxAssertion(argIndex < sd.NumMembers(), it, "too many arguments");
			//	int memberTag = sd.GetMemberTagByIndex(argIndex);
			//	int arrayItemCount = sd.GetMemberArrayItemCountOrNegative(argIndex);
			//	SingleArgumentPush(InstrValueTypeID(memberTag), it, arrayItemCount);
			//
			//	if (commaSeparated)
			//	{
			//		it.ToParent();
			//	}
			//
			//	argIndex++;
			//}
			//while (it.ToNextOrFalse());
			//
			//MS.SyntaxAssertion(!(it.HasNext()) && argIndex == numArgs, it, "wrong number of arguments");
		}

		//private bool IsFunctionOrCallback(MSText name)
		//{
		//	Context context = sem.FindContext(name);
		//	if (context == null) return ((common.callbackIDs.ContainsKey(name)));
		//	return true;
		//}
		*/

		private ArgType SingleArgumentPush(NodeIterator it, TypeDef trg = null)
		{
			MS.Verbose("Assign an argument [" + it.Data() + "]");

			//MS.Assertion(targetType < MAX_TYPES, MC.EC_INTERNAL, "invalid type");

			if (it.Type() == NodeType.EXPR)
			{
				MS.Assertion(false);
				//MS.SyntaxAssertion(!it.HasNext(), it, "argument syntax error");
				//it.ToChild();
				//NodeIterator cp = new NodeIterator(it);
				//SingleArgumentPush(targetType, cp, arrayItemCount);
				//it.ToParent();
				return null;
			}

			if (it.Type() == NodeType.HEX_TOKEN)
			{
				//if (targetType == MS_TYPE_INT)
				//{
					long number = MC.ParseHex(it.Data().GetString(), 8);
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.MS_TYPE_INT, MC.Int64lowBits(number));
					return ArgType.Data(common.IntType);
				//}
				//else if (targetType == MS_TYPE_INT64)
				//{
				//	long number = ParseHex((it.Data()).GetString(), 16);
				//	bc.AddInstruction(OP_PUSH_IMMEDIATE, 2, MS_TYPE_INT64);
				//	bc.AddWord(Int64highBits(number));
				//	bc.AddWord(Int64lowBits(number));
				//	return;
				//}
				//else
				//{
				//	MS.SyntaxAssertion(false, it, "number error");
				//}
			}
			else if (it.Type() == NodeType.NUMBER_TOKEN)
			{
				if (trg == null || trg.ID == MC.MS_TYPE_INT)
				{
					int number = MS.ParseInt((it.Data()).GetString());
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.MS_TYPE_INT, number);
					return ArgType.Data(common.IntType);
				}
				else if (trg.ID == MC.MS_TYPE_INT64)
				{
					long number = MS.ParseInt64(it.Data().GetString());
					bc.AddInstruction(MC.OP_PUSH_IMMEDIATE, 2, MC.MS_TYPE_INT64);
					bc.AddWord(MC.Int64highBits(number));
					bc.AddWord(MC.Int64lowBits(number));
					return ArgType.Data(trg);
				}
				else if (trg.ID == MC.MS_TYPE_FLOAT)
				{
					float f = MS.ParseFloat32(it.Data().GetString());
					int floatToInt = MS.FloatToIntFormat(f);
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.MS_TYPE_FLOAT, floatToInt);
					return ArgType.Data(trg);
				}
				else if (trg.ID == MC.MS_TYPE_FLOAT64)
				{
					double f = MS.ParseFloat64((it.Data()).GetString());
					long number = MS.Float64ToInt64Format(f);
					bc.AddInstruction(MC.OP_PUSH_IMMEDIATE, 2, MC.MS_TYPE_FLOAT64);
					bc.AddWord(MC.Int64highBits(number));
					bc.AddWord(MC.Int64lowBits(number));
					return ArgType.Data(trg);
				}
				else
				{
					MS.SyntaxAssertion(false, it, "number error");
				}
			}
			else if (it.Type() == NodeType.TEXT)
			{
				int textID = tree.GetTextID(it.Data());
				MS.Assertion(textID >= 0, MC.EC_INTERNAL, "text not found");
				if (trg is GenericCharsType chars)
				{
					//OP_PUSH_CHARS:
					//	int textID = bc.code[instructionPointer + 1];
					//	int maxChars = bc.code[instructionPointer + 2];
					//	int structSize = bc.code[instructionPointer + 3];

					// copy chars
					// chars.maxChars == eg. 7 if "chars[7] x"
					bc.AddInstructionWithData(MC.OP_PUSH_CHARS, 3, MC.MS_TYPE_TEXT, textID);
					bc.AddWord(chars.maxChars);
					bc.AddWord(chars.SizeOf()); // sizeOf in ints. characters + size.
					return ArgType.Data(trg);
				}
				else
				{
					// assign text id
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.MS_TYPE_TEXT, textID);
					return ArgType.Data(common.TextType);
				}
				//else
				//{
				//}
				//return;
			}
			else if (it.Type() == NodeType.NAME_TOKEN)
			{
				MS.Assertion(false);
				/*Context functionContext = sem.FindContext(it.Data());
				if (functionContext != null)
				{
					// PUSH A FUNCTION ARGUMENT

					GenerateFunctionCall(it.Copy(), functionContext);
					StructDef returnData = sem.GetStructDefType(functionContext.returnType, it);
					MS.SyntaxAssertion(targetType == returnData.typeID, it, "type mismatch");
					bc.AddInstructionWithData(OP_PUSH_REG_TO_STACK, 1, MS_TYPE_VOID, returnData.StructSize());
					return;
				}
				else if ((common.callbackIDs.ContainsKey(it.Data())))
				{

					// PUSH A CALLBACK ARGUMENT

					MCallback callback = GenerateCallbackCall(it.Copy());
					StructDef returnData = sem.GetStructDefType(callback.returnType, it);
					MS.SyntaxAssertion(targetType == returnData.typeID, it, "type mismatch");
					bc.AddInstructionWithData(OP_PUSH_REG_TO_STACK, 1, MS_TYPE_VOID, returnData.StructSize());
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
				}*/
			}

			// HANDLE THE REST IN UPPER LEVEL
			/*
			else if (it.Type() == NodeType.PARENTHESIS)
			{
				MS.Assertion(false);
				//it.ToChild();
				//NodeIterator cp = new NodeIterator(it);
				//SingleArgumentPush(targetType, cp, arrayItemCount);
				//it.ToParent();
				//return;
			}
			else if (it.Type() == NodeType.REFERENCE_TOKEN)
			{
				// TODO: SYNTAX type
				var member = currentContext.variables.GetMember(it.Data());
				int address = member.Address;
				int memberType = member.Type.ID;
				bc.AddInstructionWithData(OP_PUSH_IMMEDIATE, 1, memberType, address);
				return;
			}
			else if (it.Type() == NodeType.CODE_BLOCK)
			{
				bc.AddInstructionWithData(OP_JUMP, 1, MS_TYPE_CODE_ADDRESS, -1); // -1 to be replaced after code block is done
				int jumpAddressPosition = bc.codeTop - 1;
				int blockAddress = bc.codeTop;

				bc.AddInstruction(OP_NOOP, 0, 0); // no-op in case that the block is empty

				// generate code block

				it.ToChild();
				MS.Assertion(it.Type() == NodeType.EXPR, MC.EC_INTERNAL, "expression expected");
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
			else if (it.Type() == NodeType.SQUARE_BRACKETS)
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

					StructDef sd = sem.GetStructDefType(targetType);
					MS.SyntaxAssertion(sd != null, it, "generator: unknown type: " + targetType);
					SquareBracketArgumentPush(it.Copy(), sd, sd.NumMembers());
					return;
				}
			}*/
			MS.SyntaxAssertion(false, it, "argument error");
			return null;
		}
	}
}
