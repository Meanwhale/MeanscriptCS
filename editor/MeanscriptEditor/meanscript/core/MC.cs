namespace Meanscript
{
	public enum NodeType
	{
		ROOT,
		EXPR,
		EXPR_ASSIGN,			// a:5
		EXPR_INIT,				// int a
		EXPR_INIT_AND_ASSIGN,	// int a:5
		EXPR_FUNCTION,			// func int foo [int x] { return x }
		EXPR_STRUCT,			// struct vec [int x, int y]
		ARG,
		PARENTHESIS,
		ASSIGNMENT_BLOCK,
		SQUARE_BRACKETS,
		CODE_BLOCK,
		NAME_TOKEN,
		NUMBER_TOKEN,
		REFERENCE_TOKEN,		// e.g. "#foo", as in "increase #foo"
		REF_TYPE_TOKEN,			// e.g. "int#", as int "func void increase [int# value] { value += 1 }
		DOT,
		PLUS,
		MINUS,
		DIV,
		MUL,
		TEXT,
		MEMBER,
		COMMA,
		HEX_TOKEN,
	}
	public class Keyword
	{
		public readonly string name;
		public readonly int id;
		public readonly MSText text;

		public Keyword(string name, int id)
		{
			this.name = name;
			this.id = id;
			text = new MSText(name);
			MC.keywords.AddLast(this);
		}
	}

	public class MC
	{
		public static MList<Keyword> keywords = new MList<Keyword>();
		// node types


		// bytecode types

		public const int BYTECODE_READ_ONLY = 0x101;
		public const int BYTECODE_EXECUTABLE = 0x102;


		// instructions

		public const int OP_SYSTEM = 0x00000000; // system calls (ERROR, assert, exception, etc. TBD)
		public const int OP_CALLBACK_CALL = 0x03000000;
		public const int OP_JUMP = 0x04000000;
		public const int OP_GO_BACK = 0x05000000; // return to previous block. named to be less confusing
		public const int OP_GO_END = 0x06000000; // go to end of the function (context's end address)
												 //public const int OP_CHARS_DEF				= 0x07000000;
		public const int OP_STRUCT_DEF = 0x08000000;
		public const int OP_STRUCT_MEMBER = 0x09000000;
		public const int OP_SAVE_BASE = 0x0a000000; // save/load stack base index
		public const int OP_LOAD_BASE = 0x0b000000;
		public const int OP_NOOP = 0x0c000000;
		public const int OP_ADD_TEXT = 0x10000000; // add immutable text to text map and add index to register
		public const int OP_PUSH_IMMEDIATE = 0x11000000; // push immediate value to stack
		public const int OP_ADD_TOP = 0x12000000; // add a value on stack's top item
		public const int OP_PUSH_REG_TO_STACK = 0x13000000; // push content of register to stack
		public const int OP_FUNCTION = 0x14000000; // introduce a function
		public const int OP_START_INIT = 0x15000000;
		public const int OP_END_INIT = 0x16000000;
		public const int OP_FUNCTION_CALL = 0x17000000;
		public const int OP_PUSH_LOCAL = 0x18000000;
		public const int OP_PUSH_GLOBAL = 0x19000000;
		
		public const int OP_POP_STACK_TO_LOCAL = 0x1a000000;
		public const int OP_POP_STACK_TO_GLOBAL = 0x1b000000;
		public const int OP_POP_STACK_TO_REG = 0x1c000000;
		public const int OP_POP_STACK_TO_DYNAMIC = 0x1d000000;
		
		public const int OP_INIT_GLOBALS = 0x1e000000;
		public const int OP_GENERIC_MEMBER = 0x1f000000;
		
		public const int OP_SET_DYNAMIC_OBJECT = 0x20000000;
		
		public const int OP_POP_STACK_TO_LOCAL_REF = 0x21000000;
		public const int OP_POP_STACK_TO_GLOBAL_REF = 0x22000000;
		public const int OP_PUSH_LOCAL_REF = 0x23000000;
		public const int OP_PUSH_GLOBAL_REF = 0x24000000;
		public const int OP_PUSH_CHARS = 0x25000000;

		public const int OP_MAX = 0x30000000;
		public const int NUM_OP = 0x30;

		public static readonly string[] opName = new string[] {
			"system",               "---OLD---",            "---OLD---",            "call",
			"jump",                 "go back",              "go end",               "---OLD---",
			"struct definition",    "struct member",        "save base",            "load base",
			"no operation",         "---OLD---",            "---OLD---",            "---OLD---",
			"text",                 "push immediate",       "add top",	            "push from reg.",
			"function data",        "start init",           "end init",             "function call",
			"push local",           "push global",          "pop to local",         "pop to global",
			"pop to register",      "pop to dynamic",       "init globals",         "generic member",
			"set dynamic object",   "pop to local ref.",    "pop to global ref.",   "push local ref.",
			"push global ref.",     "push chars",           "---ERROR---",          "---ERROR---",
			"---ERROR---",          "---ERROR---",          "---ERROR---",          "---ERROR---",
			"---ERROR---",          "---ERROR---",          "---ERROR---",          "---ERROR---",
			};


		public static readonly Keyword KEYWORD_FUNC		= new Keyword("func", 0);
		public static readonly Keyword KEYWORD_STRUCT	= new Keyword("struct", 0);
		public static readonly Keyword KEYWORD_RETURN	= new Keyword("return", 0);
		public static readonly Keyword KEYWORD_GLOBAL	= new Keyword("global", 0);
		public static readonly Keyword KEYWORD_ARRAY	= new Keyword("array", 0);
		
		public const int MS_TYPE_VOID = 0; // primitive types
		public const int MS_TYPE_INT = 1;
		public const int MS_TYPE_INT64 = 2;
		public const int MS_TYPE_FLOAT = 3;
		public const int MS_TYPE_FLOAT64 = 4;
		public const int MS_TYPE_TEXT = 5;
		public const int MS_TYPE_BOOL = 6;
		public const int MS_TYPE_CODE_ADDRESS = 7;
		public const int MS_TYPE_TEXT_DATA = 8; // internal: array of text size + chars
		public const int MS_TYPE_PLUS = 9;
		public const int MS_TYPE_MINUS = 10;
		public const int MS_TYPE_MUL = 11;
		public const int MS_TYPE_DIV = 12;
		public const int MS_TYPE_NULL = 13;
		public const int MAX_MS_TYPES = 16;
		public const int MAX_TYPES = 256;

		public static readonly MSText[] primitiveNames = new MSText[] {
			new MSText("void"),
			new MSText("int"),
			new MSText("int64"),
			new MSText("float"),
			new MSText("float64"),
			new MSText("text"),
			new MSText("bool"),
			MSText.Empty(),
			MSText.Empty(),
			new MSText("chars"),
		};

		public const string HORIZONTAL_LINE = "------------------------------------------";

		public const uint OPERATION_MASK = 0xff000000;
		public const uint SIZE_MASK = 0x00ff0000; // NOTE: erikoistapauksissa voisi käyttää 0x00FFFFFF
		public const uint VALUE_TYPE_MASK = 0x0000ffff; // max. 64K

		public const uint AUX_DATA_MASK = 0x0000ffff; // same size as VALUE_TYPE_MASK for commands to use other data than value type.

		public const int OP_SHIFT = 24;
		public const int SIZE_SHIFT = 16;
		public const int VALUE_TYPE_SHIFT = 0;


		// error classes

		public static readonly MSError EC_CLASS = new MSError(null, "-");

		public static readonly MSError EC_PARSE = new MSError(EC_CLASS, "Parse error");     // when building the token tree
		public static readonly MSError EC_SYNTAX = new MSError(EC_CLASS, "Syntax error");   // ...analyzing and generating code
		public static readonly MSError EC_SCRIPT = new MSError(EC_CLASS, "Script error");   // ...executing script
		public static readonly MSError EC_CODE = new MSError(EC_CLASS, "Code error");       // ...resolving bytecode
		public static readonly MSError EC_DATA = new MSError(EC_CLASS, "Data error");       // ...accessing/creating data
		public static readonly MSError EC_TEST = new MSError(EC_CLASS, "Test error");       // ...unit test
		public static readonly MSError EC_NATIVE = new MSError(EC_CLASS, "Native error");   // ...executing native code
		public static readonly MSError EC_INTERNAL = new MSError(EC_CLASS, "Error");            // General error when executing script, accessing data, etc.

		public static readonly MSError E_UNEXPECTED_CHAR = new MSError(EC_PARSE, "Unexpected character");


		public static int MakeInstruction(int operation, int size, int auxData)
		{
			MS.Assertion((operation | OPERATION_MASK) == OPERATION_MASK, MC.EC_INTERNAL, "invalid operation");
			MS.Assertion(size >= 0 && size < 256, MC.EC_INTERNAL, "wrong size"); // TODO: size | 0xff == 0xff?
			MS.Assertion((auxData | VALUE_TYPE_MASK) == VALUE_TYPE_MASK, MC.EC_INTERNAL, "wrong aux. data");
			int sizeBits = size << SIZE_SHIFT;
			int instruction = operation | sizeBits | auxData;
			return instruction;
		}
		public static string GetOpName(int instruction)
		{
			int index = (int)(instruction & OPERATION_MASK) >> OP_SHIFT;
			index &= 0x000000ff;
			MS.Assertion(index < NUM_OP, EC_CODE, "unknown operation code");
			return opName[index];
		}

		public static bool IsArrayTag(int tag)
		{
			MS.Assertion(false);
			return false;
		}

		public static bool IsCharsTag(int tag)
		{
			MS.Assertion(false);
			return false;
		}

		public static int InstrSize(int instruction)
		{
			return (int)(instruction & SIZE_MASK) >> SIZE_SHIFT;
		}
		public static int InstrValueTypeID(int instruction)
		{
			return (int)(instruction & VALUE_TYPE_MASK) >> VALUE_TYPE_SHIFT;
		}

		public static int Int64highBits(long x)
		{
			return (int)(x >> 32);
		}
		public static int Int64lowBits(long x)
		{
			return (int)x;
		}
		public static long IntsToInt64(int high, int low)
		{
			long x = ((long)high) << 32;
			x |= ((long)low) & 0x00000000ffffffffL;
			return x;
		}

		public static void PrintBytecode(IntArray data, int top, int index, bool code)
		{
			int tagIndex = 0;
			for (int i = 0; i < top; i++)
			{
				if (code)
				{
					if (i == index) MS.Printn(">>> " + i);
					else MS.Printn("    " + i);

					if (i == tagIndex)
					{
						MS.printOut.Print(":    0x").PrintHex(data[i]).Print("      ").Print(GetOpName(data[i])).EndLine();
						tagIndex += InstrSize(data[i]) + 1;
					}
					else MS.Print(":    " + data[i]);
				}
				else
				{
					MS.Print("    " + i + ":    " + data[i]);
				}
			}
		}
		public static int StringToIntsWithSize(string text, IntArray code, int top, int maxSize)
		{
			MSText t = new MSText(text);
			int size32 = t.DataSize();
			MS.Assertion(size32 <= maxSize, EC_CODE, "text is too long, max 32-bit size: " + maxSize + ", text: " + text);
			return t.Write(code, top);
		}

		public static bool IntStringsWithSizeEquals(IntArray a, int aOffset, IntArray b, int bOffset)
		{
			// first check that sizes match
			if (a[aOffset] == b[bOffset])
			{
				// check that ints match
				int numChars = a[aOffset];
				int size = (numChars / 4) + 1;
				for (int i = 1; i <= size; i++)
				{
					if (a[aOffset + i] != b[bOffset + i]) return false;
				}
				return true;
			}
			return false;
		}

		public static void IntsToBytes(IntArray ints, int intsOffset, byte[] bytes, int bytesOffset, int bytesLength)
		{
			// order: 0x04030201

			int shift = 0;
			for (int i = 0; i < bytesLength;)
			{
				bytes[i + bytesOffset] = (byte)((ints[intsOffset + (i / 4)] >> shift) & 0x000000FF);

				i++;
				if (i % 4 == 0) shift = 0;
				else shift += 8;
			}
		}

		public static void BytesToInts(byte[] bytes, int bytesOffset, IntArray ints, int intsOffset, int bytesLength)
		{
			// TODO: tarvitaanko bytesOffset?

			// order: 0x04030201

			// bytes:	b[3] b[2] b[1] b[0] b[7] b[6] b[5] b[4]...
			// ints:	_________i[0]______|_________i[1]______...

			int shift = 0;
			ints[intsOffset] = 0;
			for (int i = 0; i < bytesLength;)
			{
				ints[(i / 4) + intsOffset] += (bytes[i] & 0x000000FF) << shift;

				i++;
				if (i % 4 == 0)
				{
					shift = 0;
					if (i < bytesLength)
					{
						ints[(i / 4) + intsOffset] = 0;
					}
				}
				else shift += 8;
			}
		}

		public static int AddTextInstruction(MSText text, int instructionCode, IntArray code, int top)
		{
			//MS.verbose("Add text: " + size32 + " x 4 bytes, " + numChars + " characters");
			int instruction = MakeInstruction(instructionCode, text.DataSize(), MS_TYPE_TEXT);
			code[top++] = instruction;
			return text.Write(code, top);
		}

		public static long ParseHex(string text, int maxChars)
		{
			int numChars = text.Length;
			MS.Assertion(numChars > 0, EC_PARSE, "empty hexadecimal literal");
			MS.Assertion(numChars <= maxChars, EC_PARSE, "hexadecimal literal is too long: " + text);
			byte[] hexes = System.Text.Encoding.UTF8.GetBytes(text);
			long x = 0;
			for (int i = 0; i < numChars; i++)
			{
				x = (x << 4) | HexCharToByte(hexes[i]);
			}
			return x;
		}

		public static byte HexCharToByte(byte c)
		{
			// TODO: tarkista onko vielä tarpeellinen
			int code = (((int)c) & 0xff);
			if (code >= '0' && code <= '9') return (byte)(code - '0');
			if (code >= 'a' && code <= 'f') return (byte)(0xa + code - 'a');
			if (code >= 'A' && code <= 'F') return (byte)(0xa + code - 'A');
			MS.errorOut.Print("invalid literal: ").PrintCharSymbol((((int)c) & 0xff)).EndLine();
			MS.Assertion(false, EC_PARSE, "wrong hex character");
			return 0;
		}

	}
}
