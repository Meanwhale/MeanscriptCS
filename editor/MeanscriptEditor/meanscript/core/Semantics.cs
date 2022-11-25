using System.Collections.Generic;

namespace Meanscript
{

	public class Semantics : MC
	{
		internal TokenTree tree;
		public int typeIDCounter;
		internal int maxContexts;
		internal int numContexts;
		internal Dictionary<MSText, int> types = new Dictionary<MSText, int>(MS.textComparer);
		internal StructDef[] typeStructDefs;
		internal Context[] contexts;
		public Context globalContext;
		internal Context currentContext;

		public Semantics(TokenTree _tree)
		{
			tree = _tree;
			typeIDCounter = MAX_MS_TYPES;

			typeStructDefs = new StructDef[MAX_TYPES];
			for (int i = 0; i < MAX_TYPES; i++)
			{
				typeStructDefs[i] = null;
			}

			maxContexts = MS.globalConfig.maxFunctions;
			contexts = new Context[maxContexts];

			contexts[0] = new Context(this, -1, 0, -1); // global context
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


		public void AddElementaryType(int typeID, int size)
		{
			string name = primitiveNames[typeID];
			MS.Verbose("Add elementary type [" + typeID + "] " + name);
			types[new MSText(name)] = typeID;
			StructDef sd = new StructDef(this, -1, typeID, size);
			typeStructDefs[typeID] = sd;
		}

		public bool HasType(MSText name)
		{
			return types.ContainsKey(name);
		}

		public bool HasType(int id)
		{
			return typeStructDefs[id] != null;
		}

		public StructDef GetType(int id)
		{
			StructDef userType = typeStructDefs[id];
			MS.Assertion(userType != null, MC.EC_INTERNAL, "Data type error");
			return userType;
		}

		public StructDef GetType(MSText name)
		{
			int id = types[name];
			StructDef userType = typeStructDefs[id];
			MS.Assertion(userType != null, MC.EC_INTERNAL, "Data type error: " + name);
			return userType;
		}

		public StructDef GetType(int id, NodeIterator itPtr)
		{
			StructDef userType = typeStructDefs[id];
			MS.SyntaxAssertion(userType != null, itPtr, "Data type error: #" + id);
			return userType;
		}

		public StructDef GetType(MSText name, NodeIterator itPtr)
		{
			int id = types[name];
			StructDef userType = typeStructDefs[id];
			MS.SyntaxAssertion(userType != null, itPtr, "Data type error: " + name);
			return userType;
		}

		public bool InGlobal()
		{
			return currentContext == contexts[0];
		}

		public int GetTextID(MSText text)
		{
			return tree.GetTextID(text);
		}

		public MSText GetText(int id)
		{
			return tree.GetTextByID(id);
		}

		public Context FindContext(MSText name)
		{
			int textID = GetTextID(name);
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
			if ((types.ContainsKey(name)))
			{
				MS.errorOut.Print("unexpected type name: " + name);
				return false;
			}

			int nameID = GetTextID(name);
			if (nameID >= 0)
			{
				// name is saved: check if it's used in contexts

				if (globalContext.variables.GetTagAddressByNameID(nameID) >= 0)
				{
					MS.errorOut.Print("duplicate variable name: " + name);
					return false;
				}
				if (currentContext != globalContext)
				{
					if (currentContext.variables.GetTagAddressByNameID(nameID) >= 0)
					{
						MS.errorOut.Print("duplicate variable name: " + name);
						return false;
					}
				}
			}

			for (int i = 0; i < NUM_KEYWORDS; i++)
			{
				if (name.Match(keywords[i]))
				{
					MS.errorOut.Print("unexpected keyword: " + name);
					return false;
				}
			}
			return true;
		}

		public void Analyze()
		{
			MS.Verbose(HORIZONTAL_LINE);
			MS.Verbose("SEMANTIC ANALYZE");
			MS.Verbose(HORIZONTAL_LINE);

			currentContext = contexts[0];

			NodeIterator it = new NodeIterator(tree.root);

			AnalyzeNode(it.Copy()); // start from the first expression

			MS.Verbose(HORIZONTAL_LINE);
			MS.Verbose("CONTEXTS");
			MS.Verbose(HORIZONTAL_LINE);
			for (int i = 0; i < maxContexts; i++)
			{
				if (contexts[i] != null)
				{
					MS.Verbose("-------- context ID: " + i);
					if (MS._verboseOn) contexts[i].variables.Info(MS.printOut);
				}
			}
			MS.Verbose(HORIZONTAL_LINE);
			MS.Verbose("END ANALYZING");
			MS.Verbose(HORIZONTAL_LINE);
		}


		public void AnalyzeNode(NodeIterator it)
		{

			while (true)
			{
				if (it.Type() == NodeType.EXPR)
				{
					if (!it.HasChild())
					{
						MS.Verbose("<EMPTY EXPR>");
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
					throw new MException(MC.EC_INTERNAL, "expression expected");
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
					MS.Verbose("-------- function call!!!");
				}
				else if (it.Data().Match(keywords[KEYWORD_FUNC_ID]))
				{
					SetParentExpr(it, NodeType.EXPR_FUNCTION);

					// get return type

					MS.SyntaxAssertion(it.HasNext(), it, "function return type expected");
					it.ToNext();
					MS.SyntaxAssertion(HasType(it.Data()), it, "unknown return type");
					int returnType = types[it.Data()];

					// function name

					MS.SyntaxAssertion(it.HasNext(), it, "function name expected");
					it.ToNext();
					MS.SyntaxAssertion(it.Type() == NodeType.NAME_TOKEN, it, "function name expected");
					MSText functionName = it.Data();

					MS.SyntaxAssertion(IsNameValidAndAvailable(functionName), it, "variable name error");

					MS.Verbose("Create a new function: " + functionName);

					// create new context
					int functionNameID = tree.AddText(functionName);
					Context funcContext = new Context(this, functionNameID, numContexts, returnType);
					contexts[numContexts++] = funcContext;

					// argument definition

					MS.SyntaxAssertion(it.HasNext(), it, "argument definition expected");
					it.ToNext();
					CreateStructDef(funcContext.variables, it.Copy());
					funcContext.variables.argsSize = funcContext.variables.structSize;
					funcContext.numArgs = funcContext.variables.numMembers;

					// parse function body

					MS.SyntaxAssertion(it.HasNext(), it, "function body expected");
					it.ToNext();
					MS.SyntaxAssertion(it.Type() == NodeType.CODE_BLOCK, it, "code block expected");

					// save node to Context for Generator
					funcContext.codeNode = it.node;

					it.ToChild();
					MS.Verbose("-------- ANALYZE FUNCTION");
					currentContext = funcContext;
					AnalyzeNode(it.Copy());
					currentContext = contexts[0];
					MS.Verbose("-------- END ANALYZE FUNCTION");
					it.ToParent();

					MS.SyntaxAssertion(!it.HasNext(), it, "unexpected token after code block");
				}
				else if (it.Data().Match(keywords[KEYWORD_STRUCT_ID]))
				{
					// e.g. "struct Vec [int x, INT y, INT z]"
					SetParentExpr(it, NodeType.EXPR_STRUCT);

					MS.SyntaxAssertion(it.HasNext(), it, "struct name expected");
					it.ToNext();
					MSText structName = it.Data();
					MS.SyntaxAssertion(IsNameValidAndAvailable(structName), it, "variable name error");
					MS.SyntaxAssertion(it.HasNext(), it, "struct definition expected");
					it.ToNext();
					MS.SyntaxAssertion(!it.HasNext(), it, "unexpected token after struct definition");
					MS.Verbose("Create a new struct: " + structName);
					AddStructDef(structName, it.Copy());
				}
				else if (HasType(it.Data()))
				{
					// expr. starts with a type name, eg. "int foo" OR "person [5] players"
					
					int type = types[it.Data()];
					MS.Assertion( // TODO: clean up?
						type == MS_TYPE_INT ||
						type == MS_TYPE_INT64 ||
						type == MS_TYPE_FLOAT ||
						type == MS_TYPE_FLOAT64 ||
						type == MS_TYPE_BOOL ||
						type == MS_TYPE_TEXT ||
						type == MS_GEN_TYPE_CHARS ||
						type >= MAX_MS_TYPES,
						MC.EC_INTERNAL, "semantics: unknown type: " + type);

					it.ToNext();

					if (type == MS_GEN_TYPE_CHARS)
					{
						// get number of chars, eg. "chars [12] name"
						MS.SyntaxAssertion(it.Type() == NodeType.SQUARE_BRACKETS, it, "chars size expected");
						it.ToChild();
						MS.SyntaxAssertion(!it.HasNext(), it, "only the chars size expected");
						it.ToChild();
						MS.SyntaxAssertion(!it.HasNext(), it, "only the chars size expected");
						MS.SyntaxAssertion(it.Type() == NodeType.NUMBER_TOKEN, it, "chars size (number) expected");

						// parse size and calculate array size

						int charCount = MS.ParseInt(it.Data().GetString());

						it.ToParent();
						it.ToParent();

						it.ToNext();
						MS.SyntaxAssertion(it.Type() == NodeType.NAME_TOKEN, it, "name expected");
						MS.SyntaxAssertion(IsNameValidAndAvailable(it.Data()), it, "variable name error");

						currentContext.variables.AddChars(tree.AddText(it.Data()), charCount);
					}
					else if (it.Type() == NodeType.SQUARE_BRACKETS)
					{
						// eg. "person [5] players"

						MS.SyntaxAssertion(InGlobal(), it, "no arrays in functions");

						// array size
						it.ToChild();
						MS.SyntaxAssertion(!it.HasNext(), it, "only the array size expected");

						int arraySize = -1;

						if (!it.HasChild())
						{
							// array size is not specified, so argument count decide it
							// eg. if "int [] numbers: 1, 2, 3" then size is 3
						}
						else
						{
							it.ToChild();
							MS.SyntaxAssertion(!it.HasNext(), it, "array size expected");
							MS.SyntaxAssertion(it.Type() == NodeType.NUMBER_TOKEN, it, "array size (number) expected");
							arraySize = MS.ParseInt(it.Data().GetString());
							MS.SyntaxAssertion(arraySize > 0 && arraySize < MS.globalConfig.maxArraySize, it, "invalid array size");
							it.ToParent();
						}
						it.ToParent();

						// array name
						it.ToNext();
						MS.SyntaxAssertion(it.Type() == NodeType.NAME_TOKEN, it, "name expected");
						MSText varName = it.Data();
						MS.SyntaxAssertion(IsNameValidAndAvailable(varName), it, "variable name error");

						if (arraySize == -1)
						{
							it.ToNext();
							MS.Assertion(it.Type() == NodeType.ASSIGNMENT, MC.EC_INTERNAL, "array assignment expected as the size is not defined");
							arraySize = it.NumChildren();
							MS.SyntaxAssertion(arraySize > 0 && arraySize < MS.globalConfig.maxArraySize, it, "invalid array size");
						}

						MS.Verbose("New array: " + varName + ", size " + arraySize);

						currentContext.variables.AddArray(tree.AddText(varName), type, arraySize);
					}
					else
					{
						// variable name
						MS.SyntaxAssertion(IsNameValidAndAvailable(it.Data()), it, "variable name error");
						MS.Verbose("New variable: " + it.Data() + " <" + GetText(currentContext.variables.nameID) + ">");
						currentContext.variables.AddMember(tree.AddText(it.Data()), type);
					}

					// check if there's an assignment or extra tokens
					if (it.HasNext())
					{
						MS.SyntaxAssertion(it.NextType() == NodeType.ASSIGNMENT, it, "unexpected token");
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
				MS.Assertion(false, EC_PARSE, "unexpected token");
			}
		}

		private void SetParentExpr(NodeIterator it, NodeType nt)
		{
			MS.Assertion(it.GetParent().type == NodeType.EXPR);
			it.GetParent().type = nt;
		}

		public void AddStructDef(MSText name, NodeIterator it)
		{
			// add user type

			int nameID = tree.AddText(name);

			int typeID = typeIDCounter++;

			StructDef sd = new StructDef(this, nameID, typeID);
			CreateStructDef(sd, it.Copy());

			sd.Info(MS.printOut);

			AddStructDef(name, typeID, sd);
		}

		public void AddStructDef(MSText name, int id, StructDef sd)
		{
			MS.Assertion(!(types.ContainsKey(name)) && typeStructDefs[sd.typeID] == null, MC.EC_INTERNAL, "addStructDef: type ID reserved");
			types[new MSText(name)] = (int)(id & VALUE_TYPE_MASK);
			typeStructDefs[sd.typeID] = sd;
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
				MS.SyntaxAssertion((types.ContainsKey((it.Data()))), it, "createStructDef: unknown type: " + it.Data());
				int type = types[(it.Data())];
				it.ToNext();

				if (type == MS_GEN_TYPE_CHARS)
				{
					// get number of chars, eg. "chars [12] name"
					MS.SyntaxAssertion(it.Type() == NodeType.SQUARE_BRACKETS, it, "chars size expected");
					it.ToChild();
					MS.SyntaxAssertion(!it.HasNext(), it, "only the chars size expected");
					it.ToChild();
					MS.SyntaxAssertion(!it.HasNext(), it, "only the chars size expected");
					MS.SyntaxAssertion(it.Type() == NodeType.NUMBER_TOKEN, it, "chars size (number) expected");

					// parse size and calculate array size

					int charCount = MS.ParseInt(it.Data().GetString());

					it.ToParent();
					it.ToParent();

					it.ToNext();
					MS.SyntaxAssertion(IsNameValidAndAvailable(it.Data()), it, "variable name error");

					sd.AddChars(tree.AddText(it.Data()), charCount);
				}
				else if (it.Type() == NodeType.SQUARE_BRACKETS)
				{
					// eg. "int [5] numbers"

					// NOTE: almost same as when defining variables...

					// array size
					it.ToChild();
					MS.SyntaxAssertion(!it.HasNext(), it, "array size expected");
					it.ToChild();
					MS.SyntaxAssertion(!it.HasNext(), it, "array size expected");
					MS.SyntaxAssertion(it.Type() == NodeType.NUMBER_TOKEN, it, "array size (number) expected");
					int arraySize = MS.ParseInt(it.Data().GetString());
					it.ToParent();
					it.ToParent();

					// array name
					it.ToNext();
					MS.Verbose("Member array: " + it.Data() + ", size" + arraySize);

					sd.AddArray(tree.AddText(it.Data()), type, arraySize);
				}
				else
				{
					MS.SyntaxAssertion(it.Type() == NodeType.NAME_TOKEN, it, "member name expected");
					MS.SyntaxAssertion(sd.GetTagAddressByName((it.Data())) < 0, it, "duplicate name: " + it.Data());
					MS.Verbose("Add struct member: " + it.Data());
					sd.AddMember(tree.AddText(it.Data()), type);
				}
				MS.SyntaxAssertion(!it.HasNext(), it, "break expected");

				it.ToParent();
			}
			while (it.ToNextOrFalse());
		}

		public void WriteStructDefs(ByteCode bc)
		{
			// write globals
			StructDef sd = contexts[0].variables;
			for (int a = 0; a < sd.codeTop; a++)
			{
				bc.AddWord(sd.code[a]);
			}

			// write user struct definitions to code
			for (int i = MAX_MS_TYPES; i < MAX_TYPES; i++)
			{
				if (typeStructDefs[i] == null) continue;
				sd = typeStructDefs[i];
				for (int a = 0; a < sd.codeTop; a++)
				{
					bc.AddWord(sd.code[a]);
				}
			}
		}
	}
}
