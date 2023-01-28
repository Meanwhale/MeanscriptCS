using System;

namespace Meanscript
{
	public class Generator
	{
		Context currentContext;
		readonly TokenTree tree;
		readonly Semantics sem;
		readonly ByteCode bc;

		public Generator(TokenTree _tree, Semantics _sem)
		{
			sem = _sem;
			bc = new ByteCode();
			tree = _tree;
			currentContext = null;
		}

		//

		private bool InGlobal()
		{
			return currentContext == sem.contexts[0];
		}

		public static ByteCode Generate(TokenTree _tree, Semantics _sem)
		{
			Generator gen = new Generator(_tree, _sem);
			gen.Generate();
			return gen.bc;
		}

		private ByteCode Generate()
		{

			MS.Verbose("------------------------ GENERATE GLOBAL CODE");

			currentContext = sem.contexts[0];

			// start
			bc.AddInstructionWithData(MC.OP_START_INIT, 2, MC.BYTECODE_EXECUTABLE, sem.texts.TextCount());
			bc.AddWord(currentContext.variables.StructSize()); // globals size

			// add texts (0 = empty)

			foreach (var textEntry in sem.texts.texts)
			{
				bc.codeTop = MC.AddTextInstruction(textEntry.Key, MC.OP_ADD_TEXT, bc.code, bc.codeTop, textEntry.Value);
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
					bc.AddWord(sem.contexts[i].variables.StructSize()); // args part of stack
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
						GenerateExpression(it.Copy(), true);
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
				else if (exprType == NodeType.EXPR_FUNCTION)
				{
					MS.Verbose("skip function");
				}
				else if (exprType == NodeType.EXPR_ASSIGN)
				{
					MS.Verbose("EXPR_ASSIGN");
					var cp = it.Copy();
					cp.ToChild();
					GenerateAssign(cp, false);
				}
				else if (exprType == NodeType.EXPR_INIT_AND_ASSIGN)
				{
					MS.Verbose("EXPR_INIT_AND_ASSIGN");
					GenerateAssignWithInit(it.Copy());
				}
				else
				{
					MS.SyntaxAssertion(false, it, "unhandled token");
				}
				if (!it.HasNext()) return;
				it.ToNext();
			}
		}

		private MList<ArgType> GenerateExpression(NodeIterator it, bool call)
		{
			MS.Assertion(it.Type() == NodeType.EXPR);
			it.ToChild();
			MS.Verbose("------------ GenerateExpression ------------");
			if (MS._verboseOn) it.PrintTree(false);

			bc.nodes.Add(bc.codeTop, it.node);

			// get list of arguments and find a call that match the list.

			// TODO: check if it's "return"

			var args = PushArgs(it);

			// try to find a callback

			var callback = sem.FindCallback(args);
			if (callback != null)
			{
				MS.Verbose("callback found: ");
				callback.Print(MS.printOut);
				bc.AddInstruction(MC.OP_CALLBACK_CALL, 0, callback.ID);
				if (callback.returnType.Def.SizeOf() > 0)
				{
					bc.AddInstructionWithData(MC.OP_PUSH_REG_TO_STACK, 1, MC.BASIC_TYPE_VOID, callback.returnType.Def.SizeOf());
				}
				args = new MList<ArgType>();
				args.Add(callback.returnType);
			}
			else if (call)
			{
				MS.SyntaxAssertion(false, it, "no function found with given arguments");
			}

			return args;
		}
		/*
		 * aikaisemmin singleargumentpushissa:
		...else if (it.Type() == NT_NAME_TOKEN)
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
		...
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
		...
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
		*/
		private void GenerateAssignWithInit(NodeIterator it)
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
			it.ToNext();

			GenerateAssign(it, true);
		}

		private void GenerateAssign(NodeIterator it, bool init)
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
			
			// push values and make conversions if available
			PushAssignValues(it, assignTarget);
			
			// stack: ... [target address][           data           ][size] top

			if (assignTarget.Ref == Arg.ADDRESS)
			{
				bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_INT, assignTarget.Def.SizeOf());
				
				if (assignTarget.Def is ObjectType)
				{
					// stack: ... [target: address to heap tag][           data           ][size] top
					bc.AddInstruction(MC.OP_POP_STACK_TO_OBJECT_TAG, 0, MC.BASIC_TYPE_VOID);
				}
				else
				{
					// stack: ... [target: heap ID + offset][           data           ][size] top
					bc.AddInstruction(MC.OP_POP_STACK_TO_OBJECT, 0, MC.BASIC_TYPE_VOID);
				}
			}
			else
			{
				MS.Assertion(false, MC.EC_INTERNAL, "stack or heap address expected");
			}
		}

		private void PushAssignValues(NodeIterator it, ArgType trg, int depth = 0)
		{
			MS.Assertion(depth < 1000);
			//Assume "it" is on assign expression.
			assignTarget = trg.Def;

			if (trg.Def is StructDefType sdt)
			{
				if (it.GetChild().type == NodeType.PARENTHESIS)
				{
					// eg. (1,2) in "person p: "Pete", (1,2), 23"
					it.ToChild();
					MS.SyntaxAssertion(!it.HasNext(), it, "stuct value syntax error");
					it.ToChild();
				}
				int numArgs = 0;
				foreach(var member in sdt.SD.members)
				{
					numArgs ++;
					
					PushAssignValues(it.Copy(), new ArgType(member.Ref, member.Type), depth + 1);
					if (numArgs < sdt.SD.NumMembers())
					{
						MS.SyntaxAssertion(it.HasNext(), it, "too few arguments");
						it.ToNext();
					}
					else
					{
						MS.SyntaxAssertion(!it.HasNext(), it, "too many arguments");
					}
				}
				
				MS.SyntaxAssertion(sdt.SD.NumMembers() == numArgs, it, "wrong number of arguments for struct: " + sdt.SD);
			}
			else if (trg.Def is GenericArrayType array)
			{
				// syntax eg. array[int,4] a: [1,2,3,4]
				it.ToChild();
				MS.SyntaxAssertion(it.Type() == NodeType.SQUARE_BRACKETS, it, "[] expected");
				it.ToChild(); // to item value expression
				int numArgs = 0;
				while(true)
				{
					numArgs++;
					PushAssignValues(it.Copy(), new ArgType(Arg.DATA, array.itemType), depth + 1);
					if (it.HasNext()) it.ToNext();
					else break;
				}
				
				MS.SyntaxAssertion(array.itemCount == numArgs, it, "wrong number of arguments for array");
			}
			else if (trg.Def is ObjectType obj)
			{
				// check if it's null
				if (it.GetChild().type == NodeType.NAME_TOKEN && it.GetChild().data.Equals("null"))
				{
					MS.SyntaxAssertion(it.GetChild().next == null, it, "extra tokens after null");
					MS.Verbose("NULL!!!");
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_INT, 0);
					return;
				}
			
				// stack for obj getter: top >>      data      >> ...

				// eg. "obj[int] p : 5"
				// read argument(s), make it a dynamic object IN THE SETTER BELOW, and assign its address
				PushAssignValues(it.Copy(), new ArgType(Arg.DATA, obj.itemType), depth + 1);

				// stack: ... [           data           ]

				// setter creates dynamic data object and saves the address tag to register.
				bc.AddInstruction(MC.OP_CALLBACK_CALL, 0, obj.SetterID);

				// save dynamic address to reg.
				bc.AddInstructionWithData(MC.OP_PUSH_REG_TO_STACK, 1, MC.BASIC_TYPE_VOID, 1);
			}
			else
			{
				var retVal = GenerateExpression(it, false);
				MS.SyntaxAssertion(retVal.Size() == 1 && retVal.First().Def == trg.Def, it, "wrong argument");
			}
		}

		private MList<ArgType> PushArgs(NodeIterator it)
		{	
			// Push expression arguments and return type list.
			// Assume "it" is on expression's first token.

			var args = new MList<ArgType>();
			bool first = true;
			while (true)
			{
				var a = ResolveAndPushArgument(it, first);
				if (a == null) break;
				MS.Verbose("ARG type: " + a);
				args.Add(a);

				if (!it.HasNext()) break;
				it.ToNext();
				first = false;
			}
			return args;
		}

		private ArgType ResolveAndPushArgument(NodeIterator it, bool first)
		{
			// move the iterator forward inside the expression

			if (it.node.type == NodeType.NAME_TOKEN)
			{
				var typeArg = sem.GetType(it.Data());
				if (typeArg != null)
				{
					if (typeArg is CallNameType cn)
					{
						MS.SyntaxAssertion(first, it, "unexpected function name");
						return ArgType.Void(cn);
					}
					
					// is NULL needed here? 

					//else if (typeArg is NullType nt)
					//{
					
					//	bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.MS_TYPE_NULL, 0);
					//	return ArgType.Dynamic(nt);
					//}
				}
				else
				{
					var arg = ResolveAndPushVariableAddress(it);

					if (arg.Ref == Arg.ADDRESS)
					{
						bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_INT, arg.Def.SizeOf());
						// OP_PUSH_GLOBAL/LOCAL gets address and size from stack and push to the stack
						MS.Assertion(InGlobal());
						bc.AddInstruction(MC.OP_PUSH_OBJECT_DATA, 0, MC.BASIC_TYPE_INT);
					}
					else
					{
						MS.Assertion(false, MC.EC_INTERNAL, "address expected");
					}

					return new ArgType(Arg.DATA, arg.Def); // return info what's on the stack now
				}
			}
			else
			{
				return SinglePrimitivePush(it);
			}
			MS.SyntaxAssertion(false, it, "unexpected argument");
			return null;
		}

		private ArgType ResolveAndPushVariableAddress(NodeIterator it)
		{	
			// muutujan osoite (head id + offset) pinon päälimmäiseksi

			var member = sem.currentContext.variables.GetMember(it.Data());
			MS.SyntaxAssertion(member != null, it, "unknown: " + it.Data()); 
			var varType = member.Type;
			int offset = member.Address;
			bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_INT, MC.MakeAddress(1, offset)); // global heap ID = 1

			while(true)
			{
				if (!it.HasNext()) break;

				if (it.GetNext().type == NodeType.ASSIGNMENT_BLOCK) break;
				
				if (varType is DynamicType)
				{
					if (varType is ObjectType obj)
					{
						// pop the dynamic object HEAP address from the stack top
						// and push it's address (head ID + offset 0) to top of the stack
						bc.AddInstruction(MC.OP_SET_DYNAMIC_OBJECT, 0, 0);
						varType = obj.itemType;
					}
					else
					{
						MS.Assertion(false);
					}
				}

				if (it.NextType() == NodeType.DOT)
				{
					// eg. "vec.x"
					it.ToNext();
					MS.SyntaxAssertion(it.HasNext(), it, "member after dot expected");
					MS.SyntaxAssertion(varType is StructDefType, it, "struct before dot expected: " + varType);
					it.ToNext();
					var structMember = ((StructDefType)varType).SD.GetMember(it.Data());
					MS.SyntaxAssertion(structMember != null, it, "unknown member: " + it.Data());
					// push offset > pop and add
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, 0, structMember.Address);
					bc.AddInstruction(MC.OP_ADD_STACK_TOP_ADDRESS_OFFSET, 0, 0);
					varType = structMember.Type;
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
					MS.SyntaxAssertion(varType is GenericArrayType, it, "array expected");
					var arrayType = (GenericArrayType)varType;
					it.ToNext();
					var cp = it.Copy();
					cp.ToChild();
					MS.SyntaxAssertion(!cp.HasNext(), cp, "only one index expected");
					cp.ToChild();
					
					// unset and set assign target
					var tmp = assignTarget;
					assignTarget = null;

					var args = PushArgs(cp);
					
					assignTarget = tmp;

					MS.SyntaxAssertion(arrayType.ValidIndex(args), it, "wrong array index");

					MS.Verbose("array callback id: " + arrayType.accessorID);
					bc.AddInstruction(MC.OP_CALLBACK_CALL, 0, arrayType.accessorID);

					// address is in register, so push it to stack

					bc.AddInstructionWithData(MC.OP_PUSH_REG_TO_STACK, 1, MC.BASIC_TYPE_VOID, 1);

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
			return ArgType.Addr(varType);
		}

		private TypeDef assignTarget = null;

		private ArgType SinglePrimitivePush(NodeIterator it)
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
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_INT, MC.Int64lowBits(number));
					return ArgType.Data(MC.basics.IntType);
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
				if (assignTarget == null || assignTarget.ID == MC.BASIC_TYPE_INT)
				{
					int number = MS.ParseInt(it.Data().GetString());
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_INT, number);
					return ArgType.Data(MC.basics.IntType);
				}
				else if (assignTarget.ID == MC.BASIC_TYPE_INT64)
				{
					long number = MS.ParseInt64(it.Data().GetString());
					bc.AddInstruction(MC.OP_PUSH_IMMEDIATE, 2, MC.BASIC_TYPE_INT64);
					bc.AddWord(MC.Int64highBits(number));
					bc.AddWord(MC.Int64lowBits(number));
					return ArgType.Data(assignTarget);
				}
				else if (assignTarget.ID == MC.BASIC_TYPE_FLOAT)
				{
					float f = MS.ParseFloat32(it.Data().GetString());
					int floatToInt = MS.FloatToIntFormat(f);
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_FLOAT, floatToInt);
					return ArgType.Data(assignTarget);
				}
				else if (assignTarget.ID == MC.BASIC_TYPE_FLOAT64)
				{
					double f = MS.ParseFloat64(it.Data().GetString());
					long number = MS.Float64ToInt64Format(f);
					bc.AddInstruction(MC.OP_PUSH_IMMEDIATE, 2, MC.BASIC_TYPE_FLOAT64);
					bc.AddWord(MC.Int64highBits(number));
					bc.AddWord(MC.Int64lowBits(number));
					return ArgType.Data(assignTarget);
				}
				else
				{
					MS.SyntaxAssertion(false, it, "type mismatch. number vs. " + assignTarget.TypeNameString());
				}
			}
			else if (it.Type() == NodeType.TEXT)
			{
				int textID = sem.texts.GetTextID(it.Data());
				MS.Assertion(textID >= 0, MC.EC_INTERNAL, "text not found");
				if (assignTarget is GenericCharsType chars)
				{
					//OP_PUSH_CHARS:
					//	int textID = bc.code[instructionPointer + 1];
					//	int maxChars = bc.code[instructionPointer + 2];
					//	int structSize = bc.code[instructionPointer + 3];

					// copy chars
					// chars.maxChars == eg. 7 if "chars[7] x"
					bc.AddInstructionWithData(MC.OP_PUSH_CHARS, 3, MC.BASIC_TYPE_TEXT, textID);
					bc.AddWord(chars.maxChars);
					bc.AddWord(chars.SizeOf()); // sizeOf in ints. characters + size.
					return ArgType.Data(assignTarget);
				}
				else
				{
					// assign text id
					bc.AddInstructionWithData(MC.OP_PUSH_IMMEDIATE, 1, MC.BASIC_TYPE_TEXT, textID);
					return ArgType.Data(MC.basics.TextType);
				}
			}
			else if (it.Type() == NodeType.NAME_TOKEN)
			{
				MS.Assertion(false);
			}
			else if (it.Type() == NodeType.PLUS)
			{
				return ArgType.Void(MC.basics.PlusOperatorType);
			}

			MS.SyntaxAssertion(false, it, "argument error");
			return null;
		}
	}
}
