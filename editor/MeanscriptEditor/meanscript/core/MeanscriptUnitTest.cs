namespace Meanscript.Core
{
	public class MeanscriptUnitTest
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

		private static void Assertion(bool b)
		{
			MS.Assertion(b, MC.EC_TEST, "");
		}

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
			MS.IntsToBytesLE(ints, 1, bytes2, 0, 5);
			Assertion(bytes2[0] == 0x61);
			Assertion(bytes2[2] == 0x63);
			Assertion(bytes2[4] == 0x65);

			byte[] cbytes = { 0x61, 0x62, 0x63, 0x64, 0x65, 0x00 };
			var ints2 = new IntArray(3);
			MS.BytesToInts(cbytes, 0, ints2, 0, 5);
			Assertion(ints2[0] == 0x64636261);
			Assertion(ints2[1] == 0x00000065);

			MSText t = new MSText(ints);
			string s = t.GetString();
			Assertion((s.Equals("abcde")));


			byte[] cbytes2 = { (byte)'a', (byte)'b', (byte)'c' };

			MSText t2 = new MSText(cbytes2, 0, 3);
			s = t2.GetString();
			Assertion((s.Equals("abc")));
			Assertion(s.Length == 3);

			MSText t3 = new MSText(""); // {0, 0}
			Assertion(t3.NumBytes() == 0);
			Assertion(t3.DataSize() == 2);
			s = t3.GetString();
			Assertion((s.Equals("")));

			var tc1 = new MSText("abc");
			var tc2 = new MSText("abcd");
			Assertion(tc1.Compare(tc1) == 0);
			Assertion(tc1.Compare(tc2) < 0);
			Assertion(tc2.Compare(tc1) > 0);
		}

		private static void Utils()
		{
			// check utility functions

			// variable name validator
			Assertion(Parser.IsValidName("abc"));
			Assertion(Parser.IsValidName("_a"));
			Assertion(Parser.IsValidName("a1"));

			Assertion(!Parser.IsValidName("123"));
			Assertion(!Parser.IsValidName("~"));
			Assertion(!Parser.IsValidName("a!"));
			Assertion(!Parser.IsValidName(""));

			// int32 max = 2147483647, int64 max = 9223372036854775808
			Assertion(MS.ParseInt("2147483647") == 2147483647);
			Assertion(MS.ParseInt64("2147483648") == 2147483648L);
			Assertion(MS.ParseInt64("-9223372036854775808") == -9223372036854775808L);
		}

		private static void Consistency()
		{
			// check that everything works similarly on all platforms

			string s = "A";
			Assertion(s.Length == 1);

			// int64 conversions

			long max = -9023372036854775808L;
			int high = MC.Int64highBits(max);
			int low = MC.Int64lowBits(max);
			long max2 = MC.IntsToInt64(high, low);
			Assertion(max == max2);

			double f64 = -12.123456789;
			long longBits = MS.Float64ToInt64Format(f64);
			Assertion(longBits == -4600357519365344569L);
			double f64x = MS.Int64FormatToFloat64(longBits);
			Assertion(f64 == f64x);
		}


		private static void SimpleVariableCheck(MSCode m)
		{
			//Assertion(m.global.HasData("a"));
			Assertion(m.global.GetInt("a") == 5);
			Assertion(m.global.GetInt64("long") == 1234567891234L);
			Assertion(m.global.GetInt64("short") == -1L);
			Assertion((m.global.GetText("b").Equals("x")));
			Assertion((m.global.GetChars("ch").Equals("asds")));
			Assertion(m.global.GetFloat("c") == -123.456f);
			Assertion(m.global.GetBool("b1") == true);
			Assertion(m.global.GetBool("b2") == false);

			Assertion(m.global.GetFloat64("d") == 12.123456789);
			string utf = m.global.GetText("utf");
			Assertion(utf.Length == 2); // A + A with umlauts
			MSText txt = new MSText(utf);
			Assertion(txt.NumBytes() == 3);
			Assertion(txt.ByteAt(0) == 0x41 && txt.ByteAt(1) == 0xc3 && txt.ByteAt(2) == 0x84);
		}

		public static void SimpleVariable()
		{
			// long max: 9223372036854775807

			MSCode m = new MSCode(simpleVariableScript);
			SimpleVariableCheck(m);
		}
		private static void Chars()
		{

			MSCode m = new MSCode("int a:5\nchars[12] c: \"Moi!\"\nint b:6");
			Assertion(m.global.GetInt("a") == 5);
			Assertion(m.global.GetChars("c").Equals("Moi!"));
			Assertion(m.global.GetInt("b") == 6);

		}
		public static void SimpleReference()
		{
			var code = "int a: 3\nint b : a\nobj[int] p: 5";

			MSCode m = new MSCode(code);
			Assertion(m.global.GetRef("p").GetInt() == 5);
		}

		public static void SimpleStruct()
		{
			const string basicStructs = "struct vec2 [int x, int y]\nstruct person [text name, vec2 point]\n";

			var code = basicStructs + "\nint a:5\nperson p: (\"Jake\", (2,3))\n";
			MSCode m = new MSCode(code);
			Assertion(m.global.GetInt("a") == 5);
			int x = m.global.GetStruct("p").GetStruct("point").GetInt("x");
			Assertion(x == 2);
			int y = m.global.GetStruct("p").GetStruct("point").GetInt("y");
			Assertion(y == 3);
			int dy = m.global.GetStruct("p").GetStruct("point").GetData("y").GetInt();
			Assertion(dy == 3);
		}

		public static void CrossReferenceStruct()
		{
			// structs that has references to themselves (by obj[x])
			// TODO: ... and others, forward and backward

			const string crossReferenceStructs = "struct vec2 [int x, int y]\nstruct company [int id, text name]\nstruct person [text name, obj [vec2] point, obj[company] workplace]\n";
			var code = crossReferenceStructs + "\nint a:5\nperson p: (\"Jake\", (2,3), (123, \"MegaCopr\"))\n";
			MSCode m = new MSCode(code);
			Assertion(m.global.GetInt("a") == 5);
			Assertion(m.global.GetStruct("p").GetRef("workplace").GetStruct().GetInt("id") == 123);
		}


		private static void SimpleFunction()
		{
			string s = "func int f [int x, int y] {\n  int n: sum x y;\n  return n }\nfunc int g [int x, int y, int z] {\n  int n: f x y;\n  return n }\nint a: g 4 3 1000";
			MSCode m = new MSCode(s);
			Assertion(m.global.GetInt("a") == 7);
		}

		public static void SimpleArray()
		{
			//const string simpleArrayScript = "int a:123\narray [int, 5] arr: [10,456,12,13,14]\nint b:456\narr[1]:11";
			const string simpleArrayScript = "int a:123\narray [int, 5] arr: [10,456,12,13,14]\nint b:arr[1]";
			MSCode m = new MSCode(simpleArrayScript);
			Assertion(m.global.GetInt("a") == 123);
			Assertion(m.global.GetInt("b") == 456);
		}

		private static void MsBuilder()
		{
			MSBuilder builder = new MSBuilder("test");

			//int personTypeID = builder.createStructDef("person");
			//builder.addMember(personTypeID, "age", MC.BASIC_TYPE_INT);
			//builder.addMember(personTypeID, "name", MC.BASIC_TYPE_TEXT);

			// struct
			// TODO: make builder for other languages too

			// simple global values
			builder.globals.AddInt("aa", 123);
			builder.globals.AddInt("bb", 456);
			//builder.addText("key", "value");

			//MSWriter pw = builder.createStruct("person", "boss");
			//pw.setInt("age", 42);
			//pw.setText("name", "Jaska");

			//builder.addArray(personTypeID, "team", 3);
			//MSWriter aw = builder.arrayItem("team", 1);
			//aw.setInt("age", 67);

			MSOutputArray output = new MSOutputArray();
			builder.Generate(output);
			
			MSInputArray input = new MSInputArray(output);
			var ms = new MSCode(input, MSCode.StreamType.BYTECODE);

			ms.PrintData();

			Assertion(ms.global.GetInt("aa") == 123);
			Assertion(ms.global.GetInt("bb") == 456);
			//Assertion(ms.getText("key").Equals("value"));

			//MSData bossData = ms.getData("boss");
			//MeanCS.print(bossData.getType());
			//MS.Assertion(bossData.getInt("age") == 42, EC_TEST, "");
			//MS.Assertion((ms.getData("boss").getText("name").Equals("Jaska")), EC_TEST, "");

			//MSDataArray arr = ms.getArray("team");
			//MS.Assertion(arr.getAt(1).getInt("age") == 67, EC_TEST, "");

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
			Assertion(m.global.GetData("p").GetMember("pos").GetMember("y").GetInt() == 34);
			Assertion(m.global.GetData("p").GetMember("pos").GetMember("x").GetInt() == 12);
			Assertion(m.global.GetData("a").GetInt() == 11);
		}

		private static void ArgumentList()
		{
			string s = testStructs;
			s += "int a: sum 2 3; int b: (sum 4 5); int c: sum (6, 7); int d: sum (sum 2 (sum (4,7))) 9";
			MSCode m = new MSCode();
			m.CompileAndRun(s);
			Assertion(m.global.GetInt("a") == 5);
			Assertion(m.global.GetInt("b") == 9);
			Assertion(m.global.GetInt("c") == 13);
			Assertion(m.global.GetInt("d") == 22);
		}

		private static void StructFunction()
		{
			string s = testStructs;
			s += "func person p [int a] { person x: [12,34], \"N\", a; return x}; vec foo; person z: p (56)";
			MSCode m = new MSCode();
			m.CompileAndRun(s);
			Assertion(m.global.GetData("z").GetInt("age") == 56);
			Assertion(m.global.GetData("z").GetMember("pos").GetInt("y") == 34);
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
			Assertion(!(m.global.HasArray("xxyyzz")));
			Assertion(m.global.HasArray("numbers"));
			Assertion(m.global.GetInt("a") == 1002);
			Assertion(m.global.GetInt("b") == 222222);
			Assertion(m.global.GetInt("c") == 222222);
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

			Assertion(m.global.GetInt("a") == 2);
			Assertion(m.global.GetInt("b") == 8888);
			Assertion(m.global.GetInt("c") == 9999);
			Assertion((m.global.GetText("t").Equals("Jaska")));

			// MSData access test

			MSDataArray arr = m.global.GetArray("team");
			Assertion(arr.GetAt(2).GetInt("age") == 9999);
			Assertion((arr.GetAt(2).GetChars("title").Equals("boss")));

			// struct array assignment
			arr = m.global.GetArray("otherTeam");
			Assertion(arr.GetAt(2).GetInt("age") == 78);
			Assertion(arr.GetAt(1).GetArray("pos").GetAt(1).GetInt("y") == 8);
		}


		private static void InputOutputStream()
		{
			string code = "int a: 5";
			MSInputArray input = new MSInputArray(code);
			MSCode m = new MSCode(input, MS.globalConfig.STREAM_SCRIPT);
			m.Run();
			Assertion(m.global.GetInt("a") == 5);

			MSOutputArray output = new MSOutputArray();
			m.WriteCode(output);

			input = new MSInputArray(output);
			m.InitBytecode(input);
			m.Run();
			Assertion(m.global.GetInt("a") == 5);
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
			MS.Print("SCRIPT OUTPUT TEST");
			m.DataOutputPrint(MS.printOut);

			s = complexStructs;
			s += "\n";
			s += output.GetString();

			m.CompileAndRun(s);
			m.DataOutputPrint(MS.printOut);

			// compare values from original script and output script
			Assertion(m.global.GetData("g").GetArray("member").GetAt(2).GetArray("corner").GetAt(0).GetChars("name").Equals("Jii"));

			SimpleVariableCheck(m);

			MS.Print("SCRIPT OUTPUT TEST ENDS");
		}
		
		*/

		private static void WriteCodeState()
		{
			MSCode m1 = new MSCode(simpleVariableScript);
			Assertion(m1.global.GetInt("a") == 5);

			MSOutputArray output = new MSOutputArray();
			m1.GenerateDataCode(output);

			var input = new MSInputArray(output);
			MSCode m2 = new MSCode(input, MSCode.StreamType.BYTECODE);
			// m.Run(); <- there's no function to run
			Assertion(m2.global.Match(m1.global));
		}

		private static bool ParseError()
		{
			try
			{
				string s = "a~";
				MSCode m = new MSCode(s);

			}
			catch (MException e) { return e.error == MC.EC_PARSE; }
			return false;
		}



		public static void RunAll()
		{
			//MS.Printn("TEST " + "NATIVE_TEST"); MS.NativeTest(); MS.Print(": OK");
			//MS.Printn("TEST " + "msText"); MsText(); MS.Print(": OK");
			//MS.Printn("TEST " + "utils"); Utils(); MS.Print(": OK");
			//MS.Printn("TEST " + "consistency"); Consistency(); MS.Print(": OK");
			//MS.Printn("TEST " + "simpleVariable"); SimpleVariable(); MS.Print(": OK");
			//MS.Printn("TEST " + "chars"); Chars(); MS.Print(": OK");
			//MS.Printn("TEST " + "simpleReference"); SimpleReference(); MS.Print(": OK");
			//MS.Printn("TEST " + "simpleStruct"); SimpleStruct(); MS.Print(": OK");
			//MS.Printn("TEST " + "crossReferenceStruct"); CrossReferenceStruct(); MS.Print(": OK");
			//MS.Printn("TEST " + "simpleArray"); SimpleArray(); MS.Print(": OK");
			//MS.Printn("TEST " + "simpleFunction"); SimpleFunction(); MS.Print(": OK");
			//MS.Printn("TEST " + "writeCodeState"); WriteCodeState(); MS.Print(": OK");

			MS.Printn("TEST " + "MsBuilder"); MsBuilder(); MS.Print(": OK");

			// MYÖHEMMIN:

			//MS.Printn("TEST " + "structAssignment"); StructAssignment(); MS.Print(": OK"); ;
			//MS.Printn("TEST " + "argumentList"); ArgumentList(); MS.Print(": OK"); ;
			//MS.Printn("TEST " + "structFunction"); StructFunction(); MS.Print(": OK"); ;
			//MS.Printn("TEST " + "msBuilder"); MsBuilder(); MS.Print(": OK"); ;
			//// TODO: test arrays when refactoring for generics is done!
			//MS.Printn("TEST " + "varArray"); VarArray(); MS.Print(": OK"); ;
			////MS.printn("TEST " +  "structArray" ); structArray(); MS.print(": OK");;
			//MS.Printn("TEST " + "inputOutputStream"); InputOutputStream(); MS.Print(": OK"); ;
			////MS.printn("TEST " +  "scriptOutput" ); scriptOutput(); MS.print(": OK");;

			// DISABLOITU MUKAVUUSSYISTÄ
			//MS.Print("TEST ERROR " + "parseError"); if (ParseError()) MS.Print(": OK"); else throw new MException(MC.EC_INTERNAL, "ERROR TEST FAIL"); ;
		}
	}
}
