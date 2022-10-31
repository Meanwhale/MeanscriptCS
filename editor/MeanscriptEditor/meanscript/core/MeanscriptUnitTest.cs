namespace Meanscript {

public class MeanscriptUnitTest : MC {


// code: struct vec [int x, int y]
public static readonly int [] vecStructCode = new int [] {
    134225936,3,6514038,486547458,1,120,151003137,0,
    1,486547458,1,121,151003137,1,1,
};
  
private static string testStructs = "struct vec [int x, int y]; struct person [vec pos, text name, int age];";

private static string simpleVariableScript = "int a: 5; int64 short: -1; int64 long: 1234567891234; text b: \"x\";chars [12] ch: \"asds\";float c:-123.456; float64 d: 12.123456789; bool b1: true; bool b2: false; text utf: \"A\\xc3\\x84\"";

private static string complexStructs = "struct vec [int x, int y, chars[7] name]\nstruct person [chars[32] name, vec [4] corner, vec pos, float age]\nstruct group [text title, person [3] member]";

private static void msText() 
{
	
	var ints = new IntArray(3);
	ints[0] = 0x00000005;
	ints[1] = 0x64636261;
	ints[2] = 0x00000065;
	
	// public static void intsToBytes(IntArray ints, int intsOffset, byte [] bytes, int bytesOffset, int bytesLength)
	// public static void bytesToInts(byte [] bytes, int bytesOffset, IntArray ints, int intsOffset, int bytesLength) 

	byte [] bytes2;
	bytes2 = new byte[10];
	intsToBytes(ints,1,bytes2,0,5);
	MS.assertion(bytes2[0] == 0x61,EC_TEST,"");
	MS.assertion(bytes2[2] == 0x63,EC_TEST,"");
	MS.assertion(bytes2[4] == 0x65,EC_TEST,"");

	byte[]cbytes = {0x61,0x62,0x63,0x64,0x65,0x00};
	var ints2 = new IntArray(3);
	bytesToInts(cbytes,0,ints2,0,5);
	MS.assertion(ints2[0] == 0x64636261,EC_TEST,"");
	MS.assertion(ints2[1] == 0x00000065,EC_TEST,"");

	MSText t = new MSText (ints);
	string s = t.getString();
	MS.assertion((s.Equals("abcde")),EC_TEST,"");
	
	
	byte[]cbytes2 = {(byte)'a',(byte)'b',(byte)'c'};
	
	MSText t2 = new MSText (cbytes2,0,3);
	s = t2.getString();
	MS.assertion((s.Equals("abc")),EC_TEST,"");
	MS.assertion(s.Length == 3,EC_TEST,"");
	
	MSText t3 = new MSText (""); // {0, 0}
	MS.assertion(t3.numBytes() == 0,EC_TEST,"");
	MS.assertion(t3.dataSize() == 2,EC_TEST,"");
	s = t3.getString();
	MS.assertion((s.Equals("")),EC_TEST,"");
}

private static void utils() 
{
	// check utility functions
	
	// variable name validator
	MS.assertion(Parser.isValidName("abc"),EC_TEST,"");
	MS.assertion(Parser.isValidName("_a"),EC_TEST,"");
	MS.assertion(Parser.isValidName("a1"),EC_TEST,"");

	MS.assertion(!Parser.isValidName("123"),EC_TEST,"");
	MS.assertion(!Parser.isValidName("~"),EC_TEST,"");
	MS.assertion(!Parser.isValidName("a!"),EC_TEST,"");
	MS.assertion(!Parser.isValidName(""),EC_TEST,"");
	
	// int32 max = 2147483647, int64 max = 9223372036854775808
	MS.assertion(MS.parseInt("2147483647") == 2147483647,EC_TEST,"");
	MS.assertion(MS.parseInt64("2147483648") == 2147483648L,EC_TEST,"");
	MS.assertion(MS.parseInt64("-9223372036854775808") == -9223372036854775808L,EC_TEST,"");
}

private static void consistency() 
{
	// check that everything works similarly on all platforms
	
	string s = "A";
	MS.assertion(s.Length == 1,EC_TEST,"");
	
	// int64 conversions

	long max = -9023372036854775808L;
	int high = int64highBits(max);
	int low = int64lowBits(max);
	long max2 = intsToInt64(high,low);
	MS.assertion(max == max2,EC_TEST,"");
	
	double f64 = -12.123456789;
	long longBits = MS.float64ToInt64Format(f64);
	MS.assertion(longBits == -4600357519365344569L,EC_TEST,"");
	double f64x = MS.int64FormatToFloat64(longBits);
	MS.assertion(f64 == f64x,EC_TEST,"");
}



private static void simpleVariableCheck(MSCode m) 
{
	MS.assertion(m.hasData("a"),EC_TEST,"");
	MS.assertion(m.getInt("a") == 5,EC_TEST,"");
	MS.assertion(m.getInt64("long") == 1234567891234L,EC_TEST,"");
	MS.assertion(m.getInt64("short") == -1L,EC_TEST,"");
	MS.assertion((m.getText("b").Equals( "x")),EC_TEST,"");
	MS.assertion((m.getChars("ch").Equals( "asds")),EC_TEST,"");
	MS.assertion(m.getFloat("c") == -123.456f,EC_TEST,"");
	MS.assertion(m.getBool("b1") == true,EC_TEST,"");
	MS.assertion(m.getBool("b2") == false,EC_TEST,"");
	
	MS.assertion(m.getFloat64("d") == 12.123456789,EC_TEST,"");
	string utf = m.getText("utf");
	MS.assertion(utf.Length == 2,EC_TEST,""); // A + A with umlauts
	MSText txt = new MSText (utf);
	MS.assertion(txt.numBytes() == 3,EC_TEST,"");
	MS.assertion(txt.byteAt(0) == 0x41 && txt.byteAt(1) == 0xc3 && txt.byteAt(2) == 0x84,EC_TEST,"");
}

private static void simpleVariable() 
{
	// long max: 9223372036854775807
	
	MSCode m = new MSCode ();
	m.compileAndRun(simpleVariableScript);
	simpleVariableCheck(m);
}

private static void structAssignment() 
{
	string s = testStructs;
	s += "person p: [11,34], \"N\", 41\n";
	s += "int a: p.pos.x;";
	s += "p.pos.x: 12\n";
	MSCode m = new MSCode();
	m.compileAndRun(s);
	MS.assertion(m.getData("p").getMember("pos").getMember("y").getInt() == 34,EC_TEST,"");
	MS.assertion(m.getData("p").getMember("pos").getMember("x").getInt() == 12,EC_TEST,"");
	MS.assertion(m.getData("a").getInt() == 11,EC_TEST,"");
	m = null;
}

private static void argumentList() 
{
	string s = testStructs;
	s += "int a: sum 2 3; int b: (sum 4 5); int c: sum (6, 7); int d: sum (sum 2 (sum (4,7))) 9";
	MSCode m = new MSCode();
	m.compileAndRun(s);
	MS.assertion(m.getInt("a") == 5,EC_TEST,"");
	MS.assertion(m.getInt("b") == 9,EC_TEST,"");
	MS.assertion(m.getInt("c") == 13,EC_TEST,"");
	MS.assertion(m.getInt("d") == 22,EC_TEST,"");
	m = null;
}

private static void simpleFunction() 
{
	string s = "func int f [int x] { int n: sum (x, 3); return n }; int a: f 5";
	MSCode m = new MSCode();
	m.compileAndRun(s);
	MS.assertion(m.getInt("a") == 8,EC_TEST,"");
	m = null;
}

private static void structFunction() 
{
	string s = testStructs;
	s += "func person p [int a] { person x: [12,34], \"N\", a; return x}; vec foo; person z: p (56)";
	MSCode m = new MSCode();
	m.compileAndRun(s);
	MS.assertion(m.getData("z").getInt("age") == 56,EC_TEST,"");
	MS.assertion(m.getData("z").getMember("pos").getInt("y") == 34,EC_TEST,"");
	m = null;
}

private static void varArray() 
{
	string s = "struct person [text name, int [2] pos, int age];";
	s += "func int summa [int a, int b] {return (sum a b)};";
	s += "int a:2;int b;int c; int [5] numbers;";
	s += "numbers[a]:1002; numbers[0]:111111; numbers[1]:1001; numbers[4]:222222;";
	s += "a: numbers[a]; b: numbers[4]; c: numbers[summa(2,2)]";
		
	MSCode m = new MSCode();
	m.compileAndRun(s);
	MS.assertion(!(m.hasArray("xxyyzz")),EC_TEST,"");
	MS.assertion(m.hasArray("numbers"),EC_TEST,"");
	MS.assertion(m.getInt("a") == 1002,EC_TEST,"");
	MS.assertion(m.getInt("b") == 222222,EC_TEST,"");
	MS.assertion(m.getInt("c") == 222222,EC_TEST,"");
	m = null;
}

private static void structArray() 
{
	string s = "struct vec [int x, int y];";
	s += "struct person [text name, chars[12] title, vec [2] pos, int age];";
	s += "func int summa [int a, int b] {return (sum a b)};";
	s += "int a:2;int b;int c:1;";
	s += "person [5] team; team[a].name: \"Jaska\"; team[a].title: \"boss\"; team[a].pos[c].x: 8888; team[a].age: 9999;";
	s += "b: team[a].pos[c].x; c: team[a].age; text t: team[a].name;";
	s += "person [] otherTeam: \n[\"A\", \"tA\", [[1,2], [3,4]], 34],\n [\"B\", \"Bt\", [[5,6], [7,8]], 56],\n [\"C\", \"tC\", [[1,2], [9,0]], 78]";
		
	MSCode m = new MSCode();
	m.compileAndRun(s);
	
	// variable test
	
	MS.assertion(m.getInt("a") == 2,EC_TEST,"");
	MS.assertion(m.getInt("b") == 8888,EC_TEST,"");
	MS.assertion(m.getInt("c") == 9999,EC_TEST,"");
	MS.assertion((m.getText("t").Equals("Jaska")),EC_TEST,"");
	
	// MSData access test
	
	MSDataArray arr = m.getArray("team");
	MS.assertion(arr.getAt(2).getInt("age") == 9999,EC_TEST,"");
	MS.assertion((arr.getAt(2).getChars("title").Equals("boss")),EC_TEST,"");
	
	// struct array assignment
	arr = m.getArray("otherTeam");
	MS.assertion(arr.getAt(2).getInt("age") == 78,EC_TEST,"");
	MS.assertion(arr.getAt(1).getArray("pos").getAt(1).getInt("y") == 8,EC_TEST,"");
	
	m = null;
	
}

private static void msBuilder() 
{
}

private static void inputOutputStream() 
{
	string code = "int a: 5";
	MSInputArray input = new MSInputArray(code);
	MSCode m = new MSCode(input, MS.globalConfig.STREAM_SCRIPT);
	m.run();
	MS.assertion(m.getInt("a") == 5,EC_TEST,"");
	
	MSOutputArray output = new MSOutputArray();
	m.writeCode(output);
	
	input = null;
	input = new MSInputArray(output);
	m.initBytecode(input);
	m.run();
	MS.assertion(m.getInt("a") == 5,EC_TEST,"");
		
	input = null;
	output = null;
	m = null;
}


private static void scriptOutput() 
{
	string s = complexStructs;
	s += "\ngroup g\ng.member[2].corner[0].x: 123\ng.member[2].corner[0].name: \"Jii\"\n";
	s += simpleVariableScript;
	MSCode m = new MSCode ();
	m.compileAndRun(s);
	
	MSOutputPrintArray output = new MSOutputPrintArray ();
	m.dataOutputPrint(output); // write only data, not structs
	
	// debug:
	MS.print("\n----------SCRIPT OUTPUT TEST\n");
	m.dataOutputPrint(MS.printOut);
	
	s = complexStructs;
	s += "\n";
	s += output.getString();
	
	m.compileAndRun(s);
	m.dataOutputPrint(MS.printOut);
	
	// compare values from original script and output script
	MS.assertion((m.getData("g").getArray("member").getAt(2).getArray("corner").getAt(0).getChars("name").Equals( "Jii")),EC_TEST,"");
	
	simpleVariableCheck(m);
	
	MS.print("\n----------SCRIPT OUTPUT TEST ENDS\n");
}


private static void writeReadOnlyData() 
{
	string code = "int a: 5";
	MSInputArray input = new MSInputArray(code);
	MSCode m = new MSCode(input, MS.globalConfig.STREAM_SCRIPT);
	m.run();
	MS.assertion(m.getInt("a") == 5,EC_TEST,"");
	
	MSOutputArray output = new MSOutputArray();
	m.writeReadOnlyData(output);
	
	input = null;
	input = new MSInputArray(output);
	m.initBytecode(input);
	MS.assertion(m.getInt("a") == 5,EC_TEST,"");
		
	input = null;
	output = null;
	m = null;
}


private static bool parseError() 
{
	try {
		MSCode m = new MSCode();
		string s = "a~";
		m.compileAndRun(s);
		
	} catch(MException e) { return e.error == EC_PARSE; }
	return false;
}



public static void  runAll () 
{
	MS.printn("TEST " +  "NATIVE_TEST" ); MS.nativeTest(); MS.print(": OK");;
	MS.printn("TEST " +  "msText" ); msText(); MS.print(": OK");;
	MS.printn("TEST " +  "utils" ); utils(); MS.print(": OK");;
	MS.printn("TEST " +  "consistency" ); consistency(); MS.print(": OK");;
	MS.printn("TEST " +  "simpleVariable" ); simpleVariable(); MS.print(": OK");;
	MS.printn("TEST " +  "structAssignment" ); structAssignment(); MS.print(": OK");;
	MS.printn("TEST " +  "argumentList" ); argumentList(); MS.print(": OK");;
	MS.printn("TEST " +  "simpleFunction" ); simpleFunction(); MS.print(": OK");;
	MS.printn("TEST " +  "structFunction" ); structFunction(); MS.print(": OK");;
	MS.printn("TEST " +  "msBuilder" ); msBuilder(); MS.print(": OK");;
	// TODO: test arrays when refactoring for generics is done!
	MS.printn("TEST " +  "varArray" ); varArray(); MS.print(": OK");;
	MS.printn("TEST " +  "structArray" ); structArray(); MS.print(": OK");;
	MS.printn("TEST " +  "inputOutputStream" ); inputOutputStream(); MS.print(": OK");;
	MS.printn("TEST " +  "writeReadOnlyData" ); writeReadOnlyData(); MS.print(": OK");;
	//MS.printn("TEST " +  "scriptOutput" ); scriptOutput(); MS.print(": OK");;


	MS.print("TEST ERROR " +  "parseError" ); if (parseError()) MS.print(": OK"); else throw new MException(MC.EC_INTERNAL,"ERROR TEST FAIL");;
	
}

}
}
