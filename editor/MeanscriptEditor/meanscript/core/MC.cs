namespace Meanscript.Core
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
		public readonly MSText text;

		public Keyword(string name)
		{
			this.name = name;
			text = new MSText(name);
			MC.keywords.AddLast(this);
		}
	}

	public sealed class MC
	{
		public static BasicTypes basics = new BasicTypes();

		public static MList<Keyword> keywords = new MList<Keyword>();

		// instructions

		public const int OP_SYSTEM = 0x00000001; // system calls (ERROR, assert, exception, etc. TBD)
		public const int OP_CALLBACK_CALL = 0x03000000;
		public const int OP_JUMP = 0x04000000;
		public const int OP_RETURN_FUNCTION = 0x05000000; // return to previous block. named to be less confusing
		public const int OP_GO_END = 0x06000000; // go to end of the function (context's end address)
												 //public const int OP_CHARS_DEF				= 0x07000000;
		public const int OP_STRUCT_DEF = 0x08000000;
		public const int OP_STRUCT_MEMBER = 0x09000000;
		//public const int OP_SAVE_BASE = 0x0a000000; // save/load stack base index
		//public const int OP_LOAD_BASE = 0x0b000000;
		public const int OP_NOOP = 0x0c000000;
		public const int OP_ADD_TEXT = 0x10000000; // add immutable text to text map and add index to register
		public const int OP_PUSH_IMMEDIATE = 0x11000000; // push immediate value to stack
		public const int OP_ADD_STACK_TOP_ADDRESS_OFFSET = 0x12000000; // add a value on stack's top item
		public const int OP_PUSH_REG_TO_STACK = 0x13000000; // push content of register to stack
		public const int OP_FUNCTION = 0x14000000; // introduce a function
		public const int OP_START_INIT = 0x15000000;
		public const int OP_END_INIT = 0x16000000;
		public const int OP_FUNCTION_CALL = 0x17000000;
		public const int OP_PUSH_CONTEXT_ADDRESS = 0x18000000;
		public const int OP_PUSH_OBJECT_DATA = 0x19000000;
		
		public const int OP_POP_STACK_TO_OBJECT = 0x1a000000;
		public const int OP_POP_STACK_TO_OBJECT_TAG = 0x1b000000;
		//public const int OP_POP_STACK_TO_LOCAL = 0x1a000000;
		//public const int OP_POP_STACK_TO_GLOBAL = 0x1b000000;
		//public const int OP_POP_STACK_TO_DYNAMIC = 0x1d000000;
		public const int OP_POP_STACK_TO_REG = 0x1c000000;
		
		public const int OP_END_DATA_INIT = 0x1d000000;
		public const int OP_WRITE_HEAP_OBJECT = 0x1e000000;
		public const int OP_GENERIC_TYPE = 0x1f000000;
		
		public const int OP_SET_DYNAMIC_OBJECT = 0x20000000;
		public const int OP_PUSH_CHARS = 0x25000000;

		public const int OP_MAX = 0x30000000;
		public const int NUM_OP = 0x30;

		public static readonly string[] opName = new string[] {
			"---OLD---",            "system",               "---OLD---",            "call",
			"jump",                 "return function",      "go end",               "---OLD---",
			"struct definition",    "struct member",        "---OLD---",            "---OLD---",
			"no operation",         "---OLD---",            "---OLD---",            "---OLD---",
			"text",                 "push immediate",       "add top",	            "push from reg.",
			"function data",        "start init",           "end init",             "function call",
			"push context address", "push object data",     "pop to object",        "pop to object tag",
			"pop to register",      "end data init",        "write heap object",    "generic type",
			"set dynamic object",   "---OLD---",            "---OLD---",            "---OLD---",
			"---OLD---",            "push chars",           "---ERROR---",          "---ERROR---",
			"---ERROR---",          "---ERROR---",          "---ERROR---",          "---ERROR---",
			"---ERROR---",          "---ERROR---",          "---ERROR---",          "---ERROR---",
			};


		public static readonly Keyword KEYWORD_FUNC		= new Keyword("func");
		public static readonly Keyword KEYWORD_STRUCT	= new Keyword("struct");
		public static readonly Keyword KEYWORD_RETURN	= new Keyword("return");
		public static readonly Keyword KEYWORD_GLOBAL	= new Keyword("global");
		public static readonly Keyword KEYWORD_ARRAY	= new Keyword("array");
		
		// primitive types
		public const int BASIC_TYPE_INT = 1;
		public const int BASIC_TYPE_INT64 = 2;
		public const int BASIC_TYPE_FLOAT = 3;
		public const int BASIC_TYPE_FLOAT64 = 4;
		public const int BASIC_TYPE_TEXT = 5;
		public const int BASIC_TYPE_BOOL = 6;
		public const int BASIC_TYPE_CODE_ADDRESS = 7;
		public const int BASIC_TYPE_TEXT_DATA = 8; // internal: array of text size + chars
		public const int BASIC_TYPE_PLUS = 9;
		public const int BASIC_TYPE_MINUS = 10;
		public const int BASIC_TYPE_MUL = 11;
		public const int BASIC_TYPE_DIV = 12;
		public const int BASIC_TYPE_NULL = 13;
		public const int BASIC_TYPE_GET = 14;
		public const int BASIC_TYPE_SET = 15;
		
		public const int BASIC_TYPE_VOID = 16;
		public const int BASIC_TYPE_GENERIC_OBJECT = 17;
		public const int BASIC_TYPE_GENERIC_ARRAY = 18;
		public const int BASIC_TYPE_GENERIC_CHARS = 19;
		
		public const int FIRST_BASIC_CALLBACK = 32;
		
		public const int MAX_BASIC_TYPES = 64;
		public const int GLOBALS_TYPE_ID = 65;
		public const int FIRST_CUSTOM_TYPE_ID = 66;
		public const int MAX_TYPES = 1024;

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

		public static int MakeAddress(int heapID, int offset)
		{
			int x = (heapID) << 16;
			x |= (offset) & 0x0000ffff;
			return x;
		}
		public static int AddressHeapID(int address)
		{
			return (address >> 16) & 0x0000ffff;
		}
		public static int AddressOffset(int address)
		{
			return address & 0x0000ffff;
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

		public static int CompareIntStringsWithSizeEquals(IntArray a, IntArray b)
		{
			// TODO: index offset, if needed
			// returns -1 (less), 1 (greater), or 0 (equal)

			if (a.Length != b.Length)
			{
				return a.Length > b.Length ? 1 : -1;
			}

			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
				{
					return a[i] > b[i] ? 1 : -1;
				}
			}
			return 0;
			// first check that sizes match
			//if (a[aOffset] == b[bOffset])
			//{
			//	// check that ints match
			//	int numChars = a[aOffset];
			//	int size = (numChars / 4) + 1;
			//	for (int i = 1; i <= size; i++)
			//	{
			//		if (a[aOffset + i] != b[bOffset + i]) return false;
			//	}
			//	return true;
			//}
			//return false;
		}

		public static int AddTextInstruction(MSText text, int instructionCode, IntArray code, int top, int textID)
		{
			//MS.verbose("add text: " + size32 + " x 4 bytes, " + numChars + " characters");
			int instruction = MakeInstruction(instructionCode, text.DataSize(), textID);
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
