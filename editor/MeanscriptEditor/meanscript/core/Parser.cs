using System.Linq;
namespace Meanscript
{

	public class Parser : MC
	{

		public const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
		public const string numbers = "1234567890";
		public const string hexNumbers = "1234567890abcdefABCDEF";
		public const string whitespace = " \t\n\r";
		public const string linebreak = "\n\r";
		public const string expressionBreak = ",;\n";
		public const string blockStart = "([{";
		public const string blockEnd = ")]}";
		public const string op = "+*<>="; // '-' or '/' will be special cases

		private static byte space, name, reference, number, hex, minus, decimalNumber, slash, quote, comment, skipLineBreaks, escapeChar, hexByte, zero;


		private static readonly byte[] tmp = new byte[4096];
		private static readonly byte[] buffer = new byte[512];
		private static readonly byte[] quoteBuffer = new byte[4096];

		private static ByteAutomata baPtr;
		private static bool goBackwards, running, assignment;
		private static int index, lastStart, lineNumber, characterNumber, quoteIndex;
		private static MNode root;
		private static MNode currentBlock;
		private static MNode currentExpr;
		private static MNode currentToken;
		private static TokenTree tokenTree;

		public static readonly string[] nodeTypeName = new string[] { "root", "expr", "sub-call", "struct", "block", "token" };


		private static void Next(byte state)
		{
			lastStart = index;

			baPtr.Next(state);
		}

		private static void NextCont(byte state)
		{
			// continue with same token,
			// so don't reset the start index
			baPtr.Next(state);
		}

		private static void Bwd()
		{
			MS.Assertion(!goBackwards, MC.EC_INTERNAL, "can't go backwards twice");
			goBackwards = true;
		}

		private static MSText GetNewName()
		{
			int start = lastStart;
			int length = index - start;
			MS.Assertion(length < MS.globalConfig.maxNameLength, EC_PARSE, "name is too long");

			int i = 0;
			for (; i < length; i++)
			{
				tmp[i] = buffer[start++ % 512];
			}
			return new MSText(tmp, 0, length);
		}

		private static MSText GetQuoteText()
		{
			int start = 0;
			int length = quoteIndex - start;
			return new MSText(quoteBuffer, start, length);
		}


		private static void AddExpr()
		{
			MNode expr = new MNode(lineNumber, characterNumber, currentBlock, NT_EXPR, new MSText("<EXPR>"));
			currentExpr.next = expr;
			currentExpr = expr;
			currentToken = null;
		}


		private static void AddToken(int tokenType)
		{
			if (tokenType == NT_REFERENCE_TOKEN) lastStart++; // skip '#'
			
			MNode token;
			if (tokenType == NT_TEXT)
			{
				MSText data = GetQuoteText();
				tokenTree.AddText(data);
				token = new MNode(lineNumber, characterNumber, currentExpr, NT_TEXT, data);
			}
			else
			{
				MSText data = GetNewName();
				{ if (MS._debug) { MS.Verbose("TOKEN: " + data); } };
				token = new MNode(lineNumber, characterNumber, currentExpr, tokenType, data);
			}

			if (currentToken == null) currentExpr.child = token;
			else currentToken.next = token;
			currentExpr.numChildren++;
			currentToken = token;
			lastStart = index;
		}
		private static void AddOperator(int tokenType, MSText name)
		{
			MNode token = new MNode(lineNumber, characterNumber, currentExpr, tokenType, name);
			if (currentToken == null) currentExpr.child = token;
			else currentToken.next = token;
			currentExpr.numChildren++;
			currentToken = token;
			lastStart = index;
		}

		private static void EndBlock(int blockType)
		{
			// check that block-end character is the right one
			MS.Assertion((currentBlock != null && currentBlock.type == blockType), EC_PARSE, "invalid block end");

			lastStart = -1;
			currentToken = currentBlock;
			currentExpr = currentToken.parent;
			currentBlock = currentExpr.parent;
		}

		private static void ExprBreak()
		{
			// check that comma is used properly
			if (baPtr.currentInput == ',')
			{
				MS.Assertion(currentBlock != null && (currentBlock.type == NT_ASSIGNMENT || currentBlock.type == NT_SQUARE_BRACKETS || currentBlock.type == NT_PARENTHESIS), EC_PARSE, "unexpected comma");
			}

			if (currentBlock != null) currentBlock.numChildren++;

			if (assignment)
			{
				if (baPtr.currentInput != ',')
				{
					// end assignment block
					// hack solution to allow assignment args without brackets
					assignment = false;
					EndBlock(NT_ASSIGNMENT);
					AddExpr();
				}
				else
				{
					// allow line breaks after comma in assignment list
					lastStart = -1;
					AddExpr();
					Next(skipLineBreaks);
				}
			}
			else
			{
				lastStart = -1;
				AddExpr();
			}
		}


		private static void AddBlock(int blockType)
		{

			MSText blockTypeName = null;

			if (blockType == NT_PARENTHESIS) blockTypeName = new MSText("<PARENTHESIS>");
			else if (blockType == NT_SQUARE_BRACKETS) blockTypeName = new MSText("<SQUARE_BRACKETS>");
			else if (blockType == NT_ASSIGNMENT)
			{
				assignment = true;
				blockTypeName = new MSText("<ASSIGNMENT>");
			}
			else if (blockType == NT_CODE_BLOCK) blockTypeName = new MSText("<CODE_BLOCK>");
			else MS.Assertion(false, MC.EC_INTERNAL, "invalid block type");

			{ if (MS._debug) { MS.Verbose("add block: " + blockType); } };

			lastStart = -1;

			MS.Assertion(blockTypeName != null, MC.EC_INTERNAL, "blockTypeName is null");

			MNode block = new MNode(lineNumber, characterNumber, currentExpr, blockType, blockTypeName);

			if (currentToken == null)
			{
				// expression starts with a block, eg. "[1,2]" in "[[1,2],34,56]"
				currentExpr.child = block;
				currentToken = block;
			}
			else
			{
				currentToken.next = block;
			}
			currentExpr.numChildren++;

			currentBlock = block;

			MNode expr = new MNode(lineNumber, characterNumber, currentBlock, NT_EXPR, new MSText("<EXPR>"));
			currentBlock.child = expr;
			currentExpr = expr;
			currentToken = null;
		}

		private static void ParseError(string msg)
		{
			MS.Printn("Parse error: ");
			MS.Print(msg);
			baPtr.ok = false;
			running = false;
		}

		private static void AddQuoteByte(int i)
		{
			if (quoteIndex >= 4096)
			{
				ParseError("text is too long");
				return;
			}
			quoteBuffer[quoteIndex++] = (byte)i;
		}

		private static void AddHexByte()
		{
			byte high = buffer[(lastStart + 1) % 512];
			byte low = buffer[(lastStart + 2) % 512];
			byte b = (byte)(((HexCharToByte(high) << 4) & 0xf0) | HexCharToByte(low));
			AddQuoteByte(b);
		}

		private static void DefineTransitions()
		{
			ByteAutomata ba = baPtr;

			space = ba.AddState("space");
			name = ba.AddState("name");
			//member = ba.addState("member");
			reference = ba.AddState("reference");
			number = ba.AddState("number");
			hex = ba.AddState("hex");
			minus = ba.AddState("minus");
			decimalNumber = ba.AddState("decimal number");
			//expNumber = ba.addState("exp. number");
			slash = ba.AddState("slash");
			quote = ba.AddState("quote");
			comment = ba.AddState("comment");
			skipLineBreaks = ba.AddState("skip line breaks");
			escapeChar = ba.AddState("escape character");
			hexByte = ba.AddState("hex byte");
			zero = ba.AddState("zero");

			ba.Transition(space, whitespace, null);
			ba.Transition(space, letters, () => { Next(name); });
			ba.Transition(space, "#", () => { Next(reference); });
			ba.Transition(space, "-", () => { Next(minus); });
			ba.Transition(space, "/", () => { Next(slash); });
			ba.Transition(space, "\"", () => { Next(quote); quoteIndex = 0; });
			ba.Transition(space, numbers, () => { Next(number); });
			ba.Transition(space, "0", () => { Next(zero); }); // start hex
			ba.Transition(space, expressionBreak, () => { ExprBreak(); });
			ba.Transition(space, "(", () => { AddBlock(NT_PARENTHESIS); Next(skipLineBreaks); });
			ba.Transition(space, ")", () => { EndBlock(NT_PARENTHESIS); });
			ba.Transition(space, "[", () => { AddBlock(NT_SQUARE_BRACKETS); Next(skipLineBreaks); });
			ba.Transition(space, "]", () => { EndBlock(NT_SQUARE_BRACKETS); });
			ba.Transition(space, "{", () => { AddBlock(NT_CODE_BLOCK); Next(skipLineBreaks); });
			ba.Transition(space, "}", () => { EndBlock(NT_CODE_BLOCK); });
			ba.Transition(space, ":", () => { AddBlock(NT_ASSIGNMENT); Next(skipLineBreaks); });
			ba.Transition(space, ".", () => { AddOperator(NT_DOT, new MSText(".")); });

			ba.Transition(name, letters, null);
			ba.Transition(name, numbers, null);
			ba.Transition(name, whitespace, () => { AddToken(NT_NAME_TOKEN); Next(space); });
			ba.Transition(name, "#", () => { AddToken(NT_REF_TYPE_TOKEN); Next(space); });
			ba.Transition(name, expressionBreak, () => { AddToken(NT_NAME_TOKEN); ExprBreak(); Next(space); });
			ba.Transition(name, blockStart, () => { AddToken(NT_NAME_TOKEN); Bwd(); Next(space); });
			ba.Transition(name, blockEnd, () => { AddToken(NT_NAME_TOKEN); Bwd(); Next(space); });
			ba.Transition(name, ":", () => { AddToken(NT_NAME_TOKEN); AddBlock(NT_ASSIGNMENT); Next(skipLineBreaks); });
			ba.Transition(name, ".", () => { AddToken(NT_NAME_TOKEN); AddOperator(NT_DOT, new MSText(".")); Next(space); });

			ba.Transition(reference, letters, null);
			ba.Transition(reference, whitespace, () => { AddToken(NT_REFERENCE_TOKEN); Next(space); });
			ba.Transition(reference, expressionBreak, () => { AddToken(NT_REFERENCE_TOKEN); ExprBreak(); Next(space); });
			ba.Transition(reference, blockStart, () => { AddToken(NT_REFERENCE_TOKEN); Bwd(); Next(space); });
			ba.Transition(reference, blockEnd, () => { AddToken(NT_REFERENCE_TOKEN); Bwd(); Next(space); });

			ba.Transition(number, numbers, null);
			ba.Transition(number, whitespace, () => { AddToken(NT_NUMBER_TOKEN); Next(space); });
			ba.Transition(number, expressionBreak, () => { AddToken(NT_NUMBER_TOKEN); ExprBreak(); Next(space); });
			ba.Transition(number, blockStart, () => { AddToken(NT_NUMBER_TOKEN); Bwd(); Next(space); });
			ba.Transition(number, blockEnd, () => { AddToken(NT_NUMBER_TOKEN); Bwd(); Next(space); });
			ba.Transition(number, ".", () => { NextCont(decimalNumber); });

			ba.Transition(zero, numbers, () => { NextCont(number); });
			ba.Transition(zero, "x", () => { Next(hex); lastStart++; });
			ba.Transition(zero, ".", () => { Next(decimalNumber); lastStart++; });
			ba.Transition(zero, whitespace, () => { AddToken(NT_NUMBER_TOKEN); Next(space); });
			ba.Transition(zero, expressionBreak, () => { AddToken(NT_NUMBER_TOKEN); ExprBreak(); Next(space); });
			ba.Transition(zero, blockStart, () => { AddToken(NT_NUMBER_TOKEN); Bwd(); Next(space); });
			ba.Transition(zero, blockEnd, () => { AddToken(NT_NUMBER_TOKEN); Bwd(); Next(space); });

			ba.Transition(hex, hexNumbers, null);
			ba.Transition(hex, whitespace, () => { AddToken(NT_HEX_TOKEN); Next(space); });
			ba.Transition(hex, expressionBreak, () => { AddToken(NT_HEX_TOKEN); ExprBreak(); Next(space); });
			ba.Transition(hex, blockStart, () => { AddToken(NT_HEX_TOKEN); Bwd(); Next(space); });
			ba.Transition(hex, blockEnd, () => { AddToken(NT_HEX_TOKEN); Bwd(); Next(space); });

			ba.Transition(minus, whitespace, () => { AddToken(NT_MINUS); Next(space); });
			ba.Transition(minus, numbers, () => { NextCont(number); }); // change state without reseting starting index
			ba.Transition(minus, ".", () => { NextCont(decimalNumber); });

			ba.Transition(decimalNumber, numbers, null);
			ba.Transition(decimalNumber, whitespace, () => { AddToken(NT_NUMBER_TOKEN); Next(space); });
			ba.Transition(decimalNumber, expressionBreak, () => { AddToken(NT_NUMBER_TOKEN); ExprBreak(); Next(space); });
			ba.Transition(decimalNumber, blockStart, () => { AddToken(NT_NUMBER_TOKEN); Bwd(); Next(space); });
			ba.Transition(decimalNumber, blockEnd, () => { AddToken(NT_NUMBER_TOKEN); Bwd(); Next(space); });

			/* decimalNumber = ba.addState("decimal number");

			   expNumber = ba.addState("exp. number");*/

			ba.FillTransition(slash, () => { AddToken(NT_DIV); Bwd(); Next(space); });
			ba.Transition(slash, "/", () => { Next(comment); });

			ba.FillTransition(quote, () => { AddQuoteByte(baPtr.currentInput); });
			ba.Transition(quote, linebreak, () => { ParseError("line break inside a quotation"); });
			ba.Transition(quote, "\"", () => { lastStart++; AddToken(NT_TEXT); Next(space); });
			ba.Transition(quote, "\\", () => { Next(escapeChar); });

			ba.FillTransition(escapeChar, () => { ParseError("invalid escape character in quotes"); });

			// standard escape character literals: https://en.cppreference.com/w/cpp/language/escape

			ba.Transition(escapeChar, "'", () => { AddQuoteByte((byte)0x27); Next(quote); });
			ba.Transition(escapeChar, "\"", () => { AddQuoteByte((byte)0x22); Next(quote); });
			ba.Transition(escapeChar, "?", () => { AddQuoteByte((byte)0x3f); Next(quote); });
			ba.Transition(escapeChar, "\\", () => { AddQuoteByte((byte)0x5c); Next(quote); });
			ba.Transition(escapeChar, "a", () => { AddQuoteByte((byte)0x07); Next(quote); });
			ba.Transition(escapeChar, "b", () => { AddQuoteByte((byte)0x08); Next(quote); });
			ba.Transition(escapeChar, "f", () => { AddQuoteByte((byte)0x0c); Next(quote); });
			ba.Transition(escapeChar, "n", () => { AddQuoteByte((byte)0x0a); Next(quote); });
			ba.Transition(escapeChar, "r", () => { AddQuoteByte((byte)0x0d); Next(quote); });
			ba.Transition(escapeChar, "t", () => { AddQuoteByte((byte)0x09); Next(quote); });
			ba.Transition(escapeChar, "v", () => { AddQuoteByte((byte)0x0b); Next(quote); });

			ba.Transition(escapeChar, "x", () => { Next(hexByte); });

			ba.FillTransition(hexByte, () => { ParseError("invalid hexadecimal byte"); });
			ba.Transition(hexByte, hexNumbers, () => { if (index - lastStart >= 2) { AddHexByte(); Next(quote); } });

			ba.FillTransition(comment, null);
			ba.Transition(comment, linebreak, () => { ExprBreak(); Next(space); });

			ba.FillTransition(skipLineBreaks, () => { Bwd(); Next(space); });
			ba.Transition(skipLineBreaks, whitespace, null);
			ba.Transition(skipLineBreaks, linebreak, null);
		}

		private static bool[] validNameChars;
		private static bool[] validNumberChars;
		private static bool nameValidatorInitialized = false;

		public static bool IsValidName(MSText name)
		{
			// name validator
			// initialize if needed

			int length, i;

			if (!nameValidatorInitialized)
			{
				byte[] letterBytes = System.Text.Encoding.UTF8.GetBytes(letters);
				byte[] numberBytes = System.Text.Encoding.UTF8.GetBytes(numbers);
				//byte[] numberBytes = System.Text.Encoding.UTF8.GetBytes(numbers);
				validNameChars = new bool[256];
				validNumberChars = new bool[256];
				string lettersString = letters;
				length = lettersString.Length;
				for (i = 0; i < length; i++)
				{
					validNameChars[letterBytes[i]] = true;
				}
				string numbersString = numbers;
				length = numbersString.Length;
				for (i = 0; i < length; i++)
				{
					validNumberChars[numberBytes[i]] = true;
				}

				nameValidatorInitialized = true;
			}

			length = name.NumBytes();

			if (length < 1 || length > MS.globalConfig.maxNameLength) return false;

			// first character must be a letter or under-score
			if (!validNameChars[name.ByteAt(0)]) return false;

			for (i = 1; i < length; i++)
			{
				int b = name.ByteAt(i);
				if (!validNameChars[b] && !validNumberChars[b]) return false;
			}

			return true;
		}

		public static bool IsValidName(string name)
		{
			MSText t = new MSText(name);
			return IsValidName(t);
		}

		public static TokenTree Parse(MSInputStream input)
		{
			MS.Verbose("------------------------ START PARSING");

			tokenTree = new TokenTree();

			baPtr = new ByteAutomata();
			ByteAutomata ba = baPtr;

			DefineTransitions();

			ba.Next((byte)1);

			root = new MNode(1, 1, null, NT_EXPR, new MSText("<ROOT>"));
			currentExpr = root;
			currentBlock = null;
			currentToken = null;

			lastStart = 0;
			running = true;
			assignment = false;
			goBackwards = false;

			lineNumber = 1;
			characterNumber = 1;
			int inputByte = 0;
			index = 0;

			while ((!input.End() || goBackwards) && running && ba.ok)
			{
				if (!goBackwards)
				{
					index++;
					inputByte = input.ReadByte();
					buffer[index % 512] = (byte)inputByte;
					if (inputByte == 10) // line break
					{
						lineNumber++;
						characterNumber = 1;
					}
					else
					{
						characterNumber++;
					}
				}
				else
				{
					goBackwards = false;
				}
				if (MS._verboseOn)
				{
					MS.printOut.Print(" [").PrintCharSymbol(inputByte).Print("]\n");
				}

				running = ba.Step(inputByte);
			}

			if (!goBackwards) index++;
			if (ba.ok) ba.Step((byte)'\n'); // ended cleanly: last command break
			if (currentBlock != null)
			{
				if (assignment) MS.Print("unexpected end of file in assignment");
				else MS.Print("unexpected end of file: closing parenthesis missing");
				ba.ok = false;
			}

			if (!running || !(ba.ok))
			{
				MS.errorOut.Print("Parser state [" + ba.stateNames[(int)ba.currentState] + "]\n");
				MS.errorOut.Print("Line " + lineNumber + ": \n        ");

				// print nearby code
				int start = index - 1;
				while (start > 0 && index - start < 512 && (char)buffer[start % 512] != '\n')
					start--;
				while (++start < index)
				{
					MS.errorOut.Print("").PrintCharSymbol((((int)buffer[start % 512]) & 0xff));
				}
				MS.errorOut.Print("\n");

				baPtr = null;
				root = null;
				tokenTree = null;
				MS.Assertion(false, EC_PARSE, null);
			}

			if (MS._verboseOn)
			{
				MS.Print("------------------------ TOKEN TREE:");
				root.PrintTree(true);
				MS.Print("------------------------ END PARSING");
			}
			baPtr = null;

			tokenTree.root = root;
			return tokenTree;
		}


	}
}
