namespace Meanscript
{

	public class MeanscriptUnitTest : MC
	{


		// code: struct vec [int x, int y]
		public static readonly int[] vecStructCode = new int[] {
			134225936,3,6514038,486547458,1,120,151003137,0,
			1,486547458,1,121,151003137,1,1,
		};

		public const string simpleStructs = "struct vec [int x, int y]; struct person [vec pos, text name, int age];";
		
		public const string simpleVariableScript = "int a: 5;\nint64 short: -1;\nint64 long: 1234567891234;\ntext b: \"x\";\nchars [12] ch: \"asds\";\nfloat c:-123.456;\nfloat64 d: 12.123456789;\nbool b1 : true;\nbool b2 : false;\ntext utf: \"A\\xc3\\x84\"";
		
		
		public const string complexStructs = "struct vec [int x, int y, chars[7] name]\nstruct person [chars[32] name, vec [4] corner, vec pos, float age]\nstruct group [text title, person [3] member]";
		public const string quiteComplexStructs = "struct vec [int x, int y]\nstruct person [array[vec,4] corner, vec pos, float age]\nstruct group [text title, array[person,3] member]" +
		                                          "\ngroup g\ng.member[1].corner[2].x: 345\nprint g.member[1].corner[2].x";
		public const string structAssign = "struct vec [int x, int y]\nstruct person [array[vec,4] corner, vec pos, float age]\nperson p: [(1,2),(1,2),(1,2),(1,2)],(1,100 + 11),12.34";
		private static void MsText()
		{

			var ints = new IntArray(3);
			ints[0] = 0x00000005;
			ints[1] = 0x64636261;
			ints[2] = 0x00000065;

			// public static void intsToBytes(IntArray ints, int intsOffset, byte [] bytes, int bytesOffset, int bytesLength)
			// public static void bytesToInts(byte [] bytes, int bytesOffset, IntArray ints, int intsOffset, int bytesLength) 

			byte[] bytes2;
			bytes2 = new byte[10];
			IntsToBytes(ints, 1, bytes2, 0, 5);
			MS.Assertion(bytes2[0] == 0x61, EC_TEST, "");
			MS.Assertion(bytes2[2] == 0x63, EC_TEST, "");
			MS.Assertion(bytes2[4] == 0x65, EC_TEST, "");

			byte[] cbytes = { 0x61, 0x62, 0x63, 0x64, 0x65, 0x00 };
			var ints2 = new IntArray(3);
			BytesToInts(cbytes, 0, ints2, 0, 5);
			MS.Assertion(ints2[0] == 0x64636261, EC_TEST, "");
			MS.Assertion(ints2[1] == 0x00000065, EC_TEST, "");

			MSText t = new MSText(ints);
			string s = t.GetString();
			MS.Assertion((s.Equals("abcde")), EC_TEST, "");


			byte[] cbytes2 = { (byte)'a', (byte)'b', (byte)'c' };

			MSText t2 = new MSText(cbytes2, 0, 3);
			s = t2.GetString();
			MS.Assertion((s.Equals("abc")), EC_TEST, "");
			MS.Assertion(s.Length == 3, EC_TEST, "");

			MSText t3 = new MSText(""); // {0, 0}
			MS.Assertion(t3.NumBytes() == 0, EC_TEST, "");
			MS.Assertion(t3.DataSize() == 2, EC_TEST, "");
			s = t3.GetString();
			MS.Assertion((s.Equals("")), EC_TEST, "");
		}

		private static void Utils()
		{
			// check utility functions

			// variable name validator
			MS.Assertion(Parser.IsValidName("abc"), EC_TEST, "");
			MS.Assertion(Parser.IsValidName("_a"), EC_TEST, "");
			MS.Assertion(Parser.IsValidName("a1"), EC_TEST, "");

			MS.Assertion(!Parser.IsValidName("123"), EC_TEST, "");
			MS.Assertion(!Parser.IsValidName("~"), EC_TEST, "");
			MS.Assertion(!Parser.IsValidName("a!"), EC_TEST, "");
			MS.Assertion(!Parser.IsValidName(""), EC_TEST, "");

			// int32 max = 2147483647, int64 max = 9223372036854775808
			MS.Assertion(MS.ParseInt("2147483647") == 2147483647, EC_TEST, "");
			MS.Assertion(MS.ParseInt64("2147483648") == 2147483648L, EC_TEST, "");
			MS.Assertion(MS.ParseInt64("-9223372036854775808") == -9223372036854775808L, EC_TEST, "");
		}

		private static void Consistency()
		{
			// check that everything works similarly on all platforms

			string s = "A";
			MS.Assertion(s.Length == 1, EC_TEST, "");

			// int64 conversions

			long max = -9023372036854775808L;
			int high = Int64highBits(max);
			int low = Int64lowBits(max);
			long max2 = IntsToInt64(high, low);
			MS.Assertion(max == max2, EC_TEST, "");

			double f64 = -12.123456789;
			long longBits = MS.Float64ToInt64Format(f64);
			MS.Assertion(longBits == -4600357519365344569L, EC_TEST, "");
			double f64x = MS.Int64FormatToFloat64(longBits);
			MS.Assertion(f64 == f64x, EC_TEST, "");
		}


		private static void SimpleVariableCheck(MSCode m)
		{
			//MS.Assertion(m.global.HasData("a"), EC_TEST, "");
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");
			MS.Assertion(m.global.GetInt64("long") == 1234567891234L, EC_TEST, "");
			MS.Assertion(m.global.GetInt64("short") == -1L, EC_TEST, "");
			MS.Assertion((m.global.GetText("b").Equals("x")), EC_TEST, "");
			MS.Assertion((m.global.GetChars("ch").Equals("asds")), EC_TEST, "");
			MS.Assertion(m.global.GetFloat("c") == -123.456f, EC_TEST, "");
			MS.Assertion(m.global.GetBool("b1") == true, EC_TEST, "");
			MS.Assertion(m.global.GetBool("b2") == false, EC_TEST, "");

			MS.Assertion(m.global.GetFloat64("d") == 12.123456789, EC_TEST, "");
			string utf = m.global.GetText("utf");
			MS.Assertion(utf.Length == 2, EC_TEST, ""); // A + A with umlauts
			MSText txt = new MSText(utf);
			MS.Assertion(txt.NumBytes() == 3, EC_TEST, "");
			MS.Assertion(txt.ByteAt(0) == 0x41 && txt.ByteAt(1) == 0xc3 && txt.ByteAt(2) == 0x84, EC_TEST, "");
		}
		
		public static void SimpleVariable()
		{
			// long max: 9223372036854775807

			MSCode m = new MSCode();
			m.CompileAndRun(simpleVariableScript);
			SimpleVariableCheck(m);
		}
		private static void Chars()
		{
			
			MSCode m = new MSCode();
			m.CompileAndRun("int a:5\nchars[12] c: \"Moi!\"\nint b:6");
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");
			MS.Assertion(m.global.GetChars("c").Equals("Moi!"), EC_TEST, "");
			MS.Assertion(m.global.GetInt("b") == 6, EC_TEST, "");

		}
		public static void SimpleReference()
		{
			var code = "int a: 3\nint b : a\nobj[int] p: 5";
			
			MSCode m = new MSCode();
			m.CompileAndRun(code);
			MS.Assertion(m.global.GetRef("p").GetInt() == 5, EC_TEST, "");
		}

		public static void SimpleStruct()
		{
			const string basicStructs = "struct vec2 [int x, int y]\nstruct person [text name, vec2 point]\n";

			var code = basicStructs + "\nint a:5\nperson p: (\"Jake\", (2,3))\n";
			MSCode m = new MSCode();
			m.CompileAndRun(code);
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");
			int x = m.global.GetStruct("p").GetStruct("point").GetInt("x");
			MS.Assertion(x == 2, EC_TEST, "");
			int y = m.global.GetStruct("p").GetStruct("point").GetInt("y");
			MS.Assertion(y == 3, EC_TEST, "");
			int dy = m.global.GetStruct("p").GetStruct("point").GetData("y").GetInt();
			MS.Assertion(dy == 3, EC_TEST, "");
		}
		
		public static void CrossReferenceStruct()
		{
			// structs that has references to themselves (by obj[x])
			// TODO: ... and others, forward and backward

			const string crossReferenceStructs = "struct vec2 [int x, int y]\nstruct company [int id, text name]\nstruct person [text name, obj [vec2] point, obj[company] workplace]\n";
			var code = crossReferenceStructs + "\nint a:5\nperson p: (\"Jake\", (2,3), (123, \"MegaCopr\"))\n";
			MSCode m = new MSCode();
			m.CompileAndRun(code);
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");
			MS.Assertion(m.global.GetStruct("p").GetRef("workplace").GetStruct().GetInt("id") == 123, EC_TEST, "");
		}
		

		private static void SimpleFunction()
		{
			string s = "func int f [int x] { int n: sum (x, 3); return n }; int a: f 5";
			MSCode m = new MSCode();
			m.CompileAndRun(s);
			MS.Assertion(m.global.GetInt("a") == 8, EC_TEST, "");
		}

		public static void SimpleArray()
		{
			const string simpleArrayScript = "int a:123\narray [int, 5] arr: [10,10,12,13,14]\nint b:456\narr[1]:11";
			MSCode m = new MSCode();
			m.CompileAndRun(simpleArrayScript);
			MS.Assertion(m.global.GetInt("a") == 123, EC_TEST, "");
			MS.Assertion(m.global.GetInt("b") == 456, EC_TEST, "");
		}
		
		/*
		private static void StructAssignment()
		{
			string s = testStructs;
			s += "person p: [11,34], \"N\", 41\n";
			s += "int a: p.pos.x;";
			s += "p.pos.x: 12\n";
			MSCode m = new MSCode();
			m.CompileAndRun(s);
			MS.Assertion(m.global.GetData("p").GetMember("pos").GetMember("y").GetInt() == 34, EC_TEST, "");
			MS.Assertion(m.global.GetData("p").GetMember("pos").GetMember("x").GetInt() == 12, EC_TEST, "");
			MS.Assertion(m.global.GetData("a").GetInt() == 11, EC_TEST, "");
		}

		private static void ArgumentList()
		{
			string s = testStructs;
			s += "int a: sum 2 3; int b: (sum 4 5); int c: sum (6, 7); int d: sum (sum 2 (sum (4,7))) 9";
			MSCode m = new MSCode();
			m.CompileAndRun(s);
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");
			MS.Assertion(m.global.GetInt("b") == 9, EC_TEST, "");
			MS.Assertion(m.global.GetInt("c") == 13, EC_TEST, "");
			MS.Assertion(m.global.GetInt("d") == 22, EC_TEST, "");
		}

		private static void StructFunction()
		{
			string s = testStructs;
			s += "func person p [int a] { person x: [12,34], \"N\", a; return x}; vec foo; person z: p (56)";
			MSCode m = new MSCode();
			m.CompileAndRun(s);
			MS.Assertion(m.global.GetData("z").GetInt("age") == 56, EC_TEST, "");
			MS.Assertion(m.global.GetData("z").GetMember("pos").GetInt("y") == 34, EC_TEST, "");
		}

		private static void VarArray()
		{
			string s = "struct person [text name, int [2] pos, int age];";
			s += "func int summa [int a, int b] {return (sum a b)};";
			s += "int a:2;int b;int c; int [5] numbers;";
			s += "numbers[a]:1002; numbers[0]:111111; numbers[1]:1001; numbers[4]:222222;";
			s += "a: numbers[a]; b: numbers[4]; c: numbers[summa(2,2)]";

			MSCode m = new MSCode();
			m.CompileAndRun(s);
			MS.Assertion(!(m.global.HasArray("xxyyzz")), EC_TEST, "");
			MS.Assertion(m.global.HasArray("numbers"), EC_TEST, "");
			MS.Assertion(m.global.GetInt("a") == 1002, EC_TEST, "");
			MS.Assertion(m.global.GetInt("b") == 222222, EC_TEST, "");
			MS.Assertion(m.global.GetInt("c") == 222222, EC_TEST, "");
		}

		private static void StructArray()
		{
			string s = "struct vec [int x, int y];";
			s += "struct person [text name, chars[12] title, vec [2] pos, int age];";
			s += "func int summa [int a, int b] {return (sum a b)};";
			s += "int a:2;int b;int c:1;";
			s += "person [5] team; team[a].name: \"Jaska\"; team[a].title: \"boss\"; team[a].pos[c].x: 8888; team[a].age: 9999;";
			s += "b: team[a].pos[c].x; c: team[a].age; text t: team[a].name;";
			s += "person [] otherTeam: \n[\"A\", \"tA\", [[1,2], [3,4]], 34],\n [\"B\", \"Bt\", [[5,6], [7,8]], 56],\n [\"C\", \"tC\", [[1,2], [9,0]], 78]";

			MSCode m = new MSCode();
			m.CompileAndRun(s);

			// variable test

			MS.Assertion(m.global.GetInt("a") == 2, EC_TEST, "");
			MS.Assertion(m.global.GetInt("b") == 8888, EC_TEST, "");
			MS.Assertion(m.global.GetInt("c") == 9999, EC_TEST, "");
			MS.Assertion((m.global.GetText("t").Equals("Jaska")), EC_TEST, "");

			// MSData access test

			MSDataArray arr = m.global.GetArray("team");
			MS.Assertion(arr.GetAt(2).GetInt("age") == 9999, EC_TEST, "");
			MS.Assertion((arr.GetAt(2).GetChars("title").Equals("boss")), EC_TEST, "");

			// struct array assignment
			arr = m.global.GetArray("otherTeam");
			MS.Assertion(arr.GetAt(2).GetInt("age") == 78, EC_TEST, "");
			MS.Assertion(arr.GetAt(1).GetArray("pos").GetAt(1).GetInt("y") == 8, EC_TEST, "");
		}

		private static void MsBuilder()
		{
		}

		private static void InputOutputStream()
		{
			string code = "int a: 5";
			MSInputArray input = new MSInputArray(code);
			MSCode m = new MSCode(input, MS.globalConfig.STREAM_SCRIPT);
			m.Run();
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");

			MSOutputArray output = new MSOutputArray();
			m.WriteCode(output);

			input = new MSInputArray(output);
			m.InitBytecode(input);
			m.Run();
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");
		}


		private static void ScriptOutput()
		{
			string s = complexStructs;
			s += "\ngroup g\ng.member[2].corner[0].x: 123\ng.member[2].corner[0].name: \"Jii\"\n";
			s += simpleVariableScript;
			MSCode m = new MSCode();
			m.CompileAndRun(s);

			MSOutputPrintArray output = new MSOutputPrintArray();
			m.DataOutputPrint(output); // write only data, not structs

			// debug:
			MS.Print("\n----------SCRIPT OUTPUT TEST\n");
			m.DataOutputPrint(MS.printOut);

			s = complexStructs;
			s += "\n";
			s += output.GetString();

			m.CompileAndRun(s);
			m.DataOutputPrint(MS.printOut);

			// compare values from original script and output script
			MS.Assertion(m.global.GetData("g").GetArray("member").GetAt(2).GetArray("corner").GetAt(0).GetChars("name").Equals("Jii"), EC_TEST, "");

			SimpleVariableCheck(m);

			MS.Print("\n----------SCRIPT OUTPUT TEST ENDS\n");
		}


		private static void WriteReadOnlyData()
		{
			string code = "int a: 5";
			MSInputArray input = new MSInputArray(code);
			MSCode m = new MSCode(input, MS.globalConfig.STREAM_SCRIPT);
			m.Run();
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");

			MSOutputArray output = new MSOutputArray();
			m.WriteReadOnlyData(output);

			input = null;
			input = new MSInputArray(output);
			m.InitBytecode(input);
			MS.Assertion(m.global.GetInt("a") == 5, EC_TEST, "");
		}

		*/
		private static bool ParseError()
		{
			try
			{
				MSCode m = new MSCode();
				string s = "a~";
				m.CompileAndRun(s);

			}
			catch (MException e) { return e.error == EC_PARSE; }
			return false;
		}



		public static void RunAll()
		{
			MS.Printn("TEST " + "NATIVE_TEST"); MS.NativeTest(); MS.Print(": OK");
			MS.Printn("TEST " + "msText"); MsText(); MS.Print(": OK");
			MS.Printn("TEST " + "utils"); Utils(); MS.Print(": OK");
			MS.Printn("TEST " + "consistency"); Consistency(); MS.Print(": OK");
			MS.Printn("TEST " + "simpleVariable"); SimpleVariable(); MS.Print(": OK");
			MS.Printn("TEST " + "chars"); Chars(); MS.Print(": OK");
			MS.Printn("TEST " + "simpleReference"); SimpleReference(); MS.Print(": OK");
			MS.Printn("TEST " + "simpleStruct"); SimpleStruct(); MS.Print(": OK");
			MS.Printn("TEST " + "crossReferenceStruct"); CrossReferenceStruct(); MS.Print(": OK");
			MS.Printn("TEST " + "simpleArray"); SimpleArray(); MS.Print(": OK");
			//MS.Printn("TEST " + "structAssignment"); StructAssignment(); MS.Print(": OK"); ;
			//MS.Printn("TEST " + "argumentList"); ArgumentList(); MS.Print(": OK"); ;
			//MS.Printn("TEST " + "simpleFunction"); SimpleFunction(); MS.Print(": OK"); ;
			//MS.Printn("TEST " + "structFunction"); StructFunction(); MS.Print(": OK"); ;
			//MS.Printn("TEST " + "msBuilder"); MsBuilder(); MS.Print(": OK"); ;
			//// TODO: test arrays when refactoring for generics is done!
			//MS.Printn("TEST " + "varArray"); VarArray(); MS.Print(": OK"); ;
			////MS.printn("TEST " +  "structArray" ); structArray(); MS.print(": OK");;
			//MS.Printn("TEST " + "inputOutputStream"); InputOutputStream(); MS.Print(": OK"); ;
			//MS.Printn("TEST " + "writeReadOnlyData"); WriteReadOnlyData(); MS.Print(": OK"); ;
			////MS.printn("TEST " +  "scriptOutput" ); scriptOutput(); MS.print(": OK");;


			// DISABLOITU MUKAVUUSSYISTÄ
			//MS.Print("TEST ERROR " + "parseError"); if (ParseError()) MS.Print(": OK"); else throw new MException(MC.EC_INTERNAL, "ERROR TEST FAIL"); ;
		}
	}
}
