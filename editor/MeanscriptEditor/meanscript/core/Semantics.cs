namespace Meanscript.Core
{

	public class Semantics : CodeTypes
	{
		internal TokenTree tree;
		internal int maxContexts;
		internal int numContexts;
		internal Context[] contexts; // TODO: use list
		public Context globalContext;
		internal Context currentContext;

		public Semantics(TokenTree _tree) : base (_tree.texts)
		{
			tree = _tree;
			tree.texts = null; // from now on, use texts in Types
			maxContexts = MS.globalConfig.maxFunctions;
			contexts = new Context[maxContexts];

			contexts[0] = new Context(this, -1, 0, null); // global context
			globalContext = contexts[0];
			currentContext = globalContext;
			for (int i = 1; i < maxContexts; i++)
			{
				contexts[i] = null;
			}
			numContexts = 1;
		}
		internal void Info(MSOutputPrint o)
		{
			o.Print("Semantics info, contexts:\n");
			foreach(var c in contexts)
				if (c != null) c.Info(o);
		}
		public bool InGlobal()
		{
			return currentContext == contexts[0];
		}

		public Context FindContext(MSText name)
		{
			int textID = texts.GetTextID(name);
			if (textID < 0) return null;
			for (int i = 1; i < maxContexts; i++)
			{
				if (contexts[i] == null) continue;
				if (contexts[i].variables.nameID == textID) return contexts[i];
			}
			return null;
		}
		
		public bool IsNameValidAndAvailable(string name)
		{
			MSText n = new MSText(name);
			return IsNameValidAndAvailable(n);
		}
		public bool IsNameValidAndAvailable(MSText name)
		{
			// check it has valid characters
			if (!Parser.IsValidName(name))
			{
				return false;
			}

			if (name.NumBytes() >= MS.globalConfig.maxNameLength)
			{
				MS.errorOut.Print("name is too long, max length: " + (MS.globalConfig.maxNameLength) + " name: " + name);
				return false;
			}
			// return true if not reserved, otherwise print error message and return false

			if (FindContext(name) != null)
			{
				MS.errorOut.Print("unexpected function name: " + name);
				return false;
			}
			if (HasDataType(name))
			{
				MS.errorOut.Print("unexpected type name: " + name);
				return false;
			}

			int nameID = texts.GetTextID(name);
			if (nameID >= 0)
			{
				// name is saved: check if it's used in contexts

				if (globalContext.variables.HasMemberByNameID(nameID))
				{
					MS.errorOut.Print("duplicate variable name: " + name);
					return false;
				}
				if (currentContext != globalContext)
				{
					if (currentContext.variables.HasMemberByNameID(nameID))
					{
						MS.errorOut.Print("duplicate variable name: " + name);
						return false;
					}
				}
			}

			foreach(var kw in MC.keywords)
			{
				if (name.Match(kw.text))
				{
					MS.errorOut.Print("unexpected keyword: " + name);
					return false;
				}
			}
			return true;
		}
		public void Analyze()
		{
			MS.Verbose(MS.Title("SEMANTIC ANALYZE"));

			currentContext = contexts[0];

			NodeIterator it = new NodeIterator(tree.root);

			AnalyzeNode(it.Copy()); // start from the first expression

			foreach(var t in types.Values)
			{
				t.Init(this);
			}

			MS.Verbose(MS.Title("CONTEXTS"));
			for (int i = 0; i < maxContexts; i++)
			{
				if (contexts[i] != null)
				{
					MS.Verbose(MS.Title("context ID: " + i));
					if (MS._verboseOn) contexts[i].variables.Info(MS.printOut);
				}
			}
			MS.Verbose(MS.Title("TYPEDEFS"));
			if (MS._verboseOn)
			{
				foreach(var t in types.Values)
				{
					MS.printOut.Print("ID: ").Print(t.ID).Print(" [").PrintHex(t.ID).Print("] ").Print(t.ToString()).EndLine();
				}
			}
			MS.Verbose(MS.Title("END ANALYZING"));
		}


		public void AnalyzeNode(NodeIterator it)
		{

			while (true)
			{
				if (it.Type() == NodeType.EXPR || it.Type() == NodeType.EXPR_ASSIGN)
				{
					if (!it.HasChild())
					{
						MS.Verbose("---- EMPTY EXPR ----");
					}
					else
					{
						// make a new iterator for child
						it.ToChild();
						AnalyzeExpr(it.Copy());
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

		public void AnalyzeExpr(NodeIterator it)
		{
			if (MS._verboseOn) it.PrintTree(false);

			if (it.Type() == NodeType.NAME_TOKEN)
			{
				Context context = FindContext(it.Data());

				if (context != null)
				{
					MS.Verbose(MS.Title("function call"));
				}
				else if (it.Data().Match(MC.KEYWORD_FUNC.text))
				{
					SetParentExpr(it, NodeType.EXPR_FUNCTION);

					// get return type

					MS.SyntaxAssertion(it.HasNext(), it, "function return type expected");
					it.ToNext();
					MS.SyntaxAssertion(HasDataType(it.Data()), it, "unknown return type");
					var returnType = GetDataType(it.Data());

					// function name

					MS.SyntaxAssertion(it.HasNext(), it, "function name expected");
					it.ToNext();
					MS.SyntaxAssertion(it.Type() == NodeType.NAME_TOKEN, it, "function name expected");
					MSText functionName = it.Data();

					MS.SyntaxAssertion(IsNameValidAndAvailable(functionName), it, "variable name error");

					MS.Verbose("create a new function: " + functionName);

					// create new context
					int functionNameID = texts.AddText(functionName);
					Context funcContext = new Context(this, functionNameID, numContexts, returnType);
					contexts[numContexts++] = funcContext;
					
					AddTypeDef(new ScriptedFunctionNameType(GetNewTypeID(), functionName, funcContext));

					// argument definition

					MS.SyntaxAssertion(it.HasNext(), it, "argument definition expected");
					it.ToNext();
					CreateStructDef(funcContext.variables, it.Copy());
					funcContext.argsSize = funcContext.variables.StructSize();

					// parse function body

					MS.SyntaxAssertion(it.HasNext(), it, "function body expected");
					it.ToNext();
					MS.SyntaxAssertion(it.Type() == NodeType.CODE_BLOCK, it, "code block expected");

					// save node to Context for Generator
					funcContext.codeNode = it.node;

					it.ToChild();
					MS.Verbose(MS.Title("ANALYZE FUNCTION"));
					currentContext = funcContext;
					AnalyzeNode(it.Copy());
					currentContext = contexts[0];
					MS.Verbose(MS.Title("END ANALYZE FUNCTION"));
					it.ToParent();

					MS.SyntaxAssertion(!it.HasNext(), it, "unexpected token after code block");
				}
				else if (it.Data().Match(MC.KEYWORD_STRUCT.text))
				{
					// e.g. "struct Vec2 [int x, int y]"
					SetParentExpr(it, NodeType.EXPR_STRUCT);

					MS.SyntaxAssertion(it.HasNext(), it, "struct name expected");
					it.ToNext();
					MSText structName = it.Data();
					MS.SyntaxAssertion(IsNameValidAndAvailable(structName), it, "variable name error");
					MS.SyntaxAssertion(it.HasNext(), it, "struct definition expected");
					it.ToNext();
					MS.SyntaxAssertion(!it.HasNext(), it, "unexpected token after struct definition");
					MS.Verbose("create a new struct: " + structName);
					AddStructDef(structName, it.Copy());
				}
				else if (HasGenericType(it.Data()))
				{
					//  eg. "array [person,5] players"
					AddGeneric(it, currentContext.variables);
				}
				else if (HasDataType(it.Data()))
				{
					// eg. "int foo" pr "person p"
					
					var dataType = GetDataType(it.Data());
					int typeID = dataType.ID;
					MS.Assertion( // TODO: clean up?
						typeID == MC.BASIC_TYPE_INT ||
						typeID == MC.BASIC_TYPE_INT64 ||
						typeID == MC.BASIC_TYPE_FLOAT ||
						typeID == MC.BASIC_TYPE_FLOAT64 ||
						typeID == MC.BASIC_TYPE_BOOL ||
						typeID == MC.BASIC_TYPE_TEXT ||
						typeID >= MC.FIRST_CUSTOM_TYPE_ID,
						MC.EC_INTERNAL, "semantics: unknown type: " + typeID);

					it.ToNext();

					// variable name
					MS.SyntaxAssertion(IsNameValidAndAvailable(it.Data()), it, "variable name error");
					MS.Verbose("new variable: " + dataType.TypeNameString() + " " + it.Data());
					currentContext.variables.AddMember(texts.AddText(it.Data()), dataType, Arg.DATA);

					// check if there's an assignment or extra tokens
					if (it.HasNext())
					{
						MS.SyntaxAssertion(it.NextType() == NodeType.ASSIGNMENT_BLOCK, it, "unexpected token");
						SetParentExpr(it, NodeType.EXPR_INIT_AND_ASSIGN);
					}
					else
					{
						SetParentExpr(it, NodeType.EXPR_INIT);
					}
				}
			}
			else
			{
				MS.Assertion(false, MC.EC_PARSE, "unexpected token");
			}
		}

		private void AddGeneric(NodeIterator it, StructDef sd)
		{
			var genTypeName = it.Data();
			// "array [int,5] a" --> 
			//[<EXPR>]
			//  [array]
			//  [<SQUARE_BRACKETS>]
			//		[<EXPR>]
			//		  [int]
			//		[<EXPR>]
			//		  [5]
			//	[a]
			it.ToNext(NodeType.SQUARE_BRACKETS);
			it.ToChild();
			MS.Assertion(it.NumChildren() == 1, MC.EC_SYNTAX, "arguments expected");
			var genArgs = new MList<MNode>();
			while(true)
			{
				// read argument nodes
				it.ToChild();
				genArgs.AddLast(it.node);
				it.ToParent();
				if (!it.HasNext()) break;
				it.ToNext();
			}
			it.ToParent();
			it.ToNext(NodeType.NAME_TOKEN);
					
			MS.SyntaxAssertion(IsNameValidAndAvailable(it.Data()), it, "variable name error");
			MS.Verbose("new variable: " + it.Data() + " <" + texts.GetText(currentContext.variables.nameID) + ">");
			var genArgType = CreateGenericVariable(genTypeName, genArgs, it);
			sd.AddMember(texts.AddText(it.Data()), genArgType);
			
			if (it.HasNext())
			{
				MS.Assertion(it.GetNext().type == NodeType.ASSIGNMENT_BLOCK, MC.EC_SYNTAX, "generic type: extra tokens after name");
				SetParentExpr(it, NodeType.EXPR_INIT_AND_ASSIGN);
			}
			else
			{
				SetParentExpr(it, NodeType.EXPR_INIT);
			}
		}

		private ArgType CreateGenericVariable(MSText genTypeName, MList<MNode> genArgs, NodeIterator it)
		{

			var factory = GetGenericFactory(genTypeName);			
			MS.Assertion(factory != null);
			var ot = factory.Create(GetNewTypeID(), genArgs, this, it);
			AddTypeDef(ot);

			// TODO: smart way to decide ref. type = size
			if (ot is ObjectType) return new ArgType(Arg.ADDRESS, ot);
			return new ArgType(Arg.DATA, ot);
		}

		private bool HasGenericType(MSText name)
		{
			return GetGenericFactory(name) != null;
		}

		private GenericFactory GetGenericFactory(MSText name)
		{
			foreach(var f in GenericFactory.factories)
			{
				if (name.Equals(f.TypeName())) return f;
			}
			return null;
		}

		private void SetParentExpr(NodeIterator it, NodeType nt)
		{
			MS.Assertion(it.GetParent().type == NodeType.EXPR || it.GetParent().type == NodeType.EXPR_ASSIGN);
			it.GetParent().type = nt;
		}

		public void AddStructDef(MSText name, NodeIterator it)
		{
			// add user type

			int nameID = texts.AddText(name);

			StructDef sd = new StructDef(this, nameID);
			
			CreateStructDef(sd, it.Copy());
			
			AddStructDef(nameID, sd);

			sd.Info(MS.printOut);
			
		}

		public void AddStructDef(int nameID, StructDef sd)
		{
			AddTypeDef(new StructDefType(GetNewTypeID(), texts.GetText(nameID), sd));
		}

		public void CreateStructDef(StructDef sd, NodeIterator it)
		{
			MS.SyntaxAssertion(it.Type() == NodeType.SQUARE_BRACKETS, it, "struct definition expected");

			it.ToChild();
			MS.SyntaxAssertion(it.Type() == NodeType.EXPR, it, "exression expected");

			do
			{
				if (!it.HasChild()) continue; // skip an empty expression
				it.ToChild();

				if (HasGenericType(it.Data()))
				{
					AddGeneric(it.Copy(), sd);
				}
				else
				{
					MS.SyntaxAssertion(HasDataType(it.Data()), it, "createStructDef: unknown type: " + it.Data());
					var dataType = GetDataType(it.Data());
					it.ToNext();

					MS.SyntaxAssertion(it.Type() == NodeType.NAME_TOKEN, it, "member name expected");
					MS.SyntaxAssertion(!sd.HasMember(it.Data()), it, "duplicate name: " + it.Data());
					MS.Verbose("add struct member: " + it.Data());
					sd.AddMember(texts.AddText(it.Data()), dataType, Arg.DATA);
					MS.SyntaxAssertion(!it.HasNext(), it, "break expected");
				}
				
				it.ToParent();
			}
			while (it.ToNextOrFalse());
		}

		public void WriteTypes(ByteCode bc)
		{
			// encode globals and StructDefs
			
			foreach(var tv in types)
			{
				if (tv.Value is ObjectType ot)
				{
					var factory = GenericFactory.Get(MC.BASIC_TYPE_GENERIC_OBJECT);
					factory.Encode(bc, ot);
				}
				if (tv.Value is StructDefType sdt)
				{
					sdt.SD.Encode(bc, tv.Key);
				}
				if (tv.Value is GenericArrayType at)
				{
					var factory = GenericFactory.Get(MC.BASIC_TYPE_GENERIC_ARRAY);
					factory.Encode(bc, at);
				}
				if (tv.Value is GenericCharsType ct)
				{
					var factory = GenericFactory.Get(MC.BASIC_TYPE_GENERIC_CHARS);
					factory.Encode(bc, ct);
				}
			}
			globalContext.variables.Encode(bc, MC.GLOBALS_TYPE_ID);
			

			// write globals
			//StructDef sd = contexts[0].variables;
			//for (int a = 0; a < sd.codeTop; a++)
			//{
			//	bc.AddWord(sd.code[a]);
			//}
			//
			//// write user struct definitions to code
			//for (int i = MAX_MS_TYPES; i < MAX_TYPES; i++)
			//{
			//	if (typeStructDefs[i] == null) continue;
			//	sd = typeStructDefs[i];
			//	for (int a = 0; a < sd.codeTop; a++)
			//	{
			//		bc.AddWord(sd.code[a]);
			//	}
			//}
		}
	}
}
