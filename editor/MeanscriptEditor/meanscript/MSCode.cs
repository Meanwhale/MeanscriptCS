namespace Meanscript {

public class MSCode : MC {
Common common;

MeanMachine mm;

bool initialized;

public MSCode () 
{
	common = new Common();
	mm = null;
	initialized = false;
	
	reset();
}
public MSCode (MSInputStream input, int streamType) 
{
	MS.assertion(streamType >= MS.globalConfig.STREAM_TYPE_FIRST && streamType <= MS.globalConfig.STREAM_TYPE_LAST,MC.EC_INTERNAL, "unknown stream type");
	
	common = new Common();
	mm = null;
	initialized = false;
	
	if (streamType == MS.globalConfig.STREAM_BYTECODE)
	{
		initBytecode(input);
	}
	else if (streamType == MS.globalConfig.STREAM_SCRIPT)
	{
		mm = new MeanMachine(compile(input));
	}
	else
	{
		MS.assertion(false,MC.EC_INTERNAL, "unknown stream type");
	}
}

public MeanMachine getMM ()
{
	return mm;
}

//;

public void reset ()
{
	mm = null;
	initialized = false;
}

public bool isInitialized ()
{
	return initialized;
}

public void checkInit () 
{
	MS.assertion(initialized,MC.EC_INTERNAL, "MSCode is not initialized");
}

public void initBytecode (MSInputStream input) 
{
	reset();
	
	ByteCode byteCode = new ByteCode(common, input);
	mm = new MeanMachine(byteCode);
	
	initialized = true;
}

public void initBytecode (ByteCode bc) 
{
	reset();
	
	ByteCode byteCode = new ByteCode(bc);
	mm = new MeanMachine(byteCode);
	
	initialized = true;
}

public bool hasData (string name) 
{
	return mm.globals.hasData(name);
}

public bool hasArray (string name) 
{
	return mm.globals.hasArray(name);
}

public bool getBool (string name) 
{
	checkInit();
	return mm.globals.getBool(name);
}

public int getInt (string name) 
{
	checkInit();
	return mm.globals.getInt(name);
}

public long getInt64 (string name) 
{
	checkInit();
	return mm.globals.getInt64(name);
}

public float getFloat (string name) 
{
	checkInit();
	return mm.globals.getFloat(name);
}
public double getFloat64 (string name) 
{
	checkInit();
	return mm.globals.getFloat64(name);
}

public string getText (string name) 
{
	checkInit();
	return mm.globals.getText(name);
}

public string getChars (string name) 
{
	checkInit();
	return mm.globals.getChars(name);
}

public string getText (int textID) 
{
	checkInit();
	return mm.globals.getText(textID);
}

public MSData getData (string name) 
{
	checkInit();
	return mm.globals.getMember(name);
}

public MSDataArray getArray (string name) 
{
	checkInit();
	return mm.globals.getArray(name);
}

public void writeReadOnlyData (MSOutputStream output) 
{
	mm.writeReadOnlyData(output);
}

public void writeCode (MSOutputStream output) 
{
	mm.writeCode(output);
}

public void printCode () 
{
	if (initialized) mm.printCode();
	else MS.print("printCode: MSCode is not initialized");
}

public void printDetails () 
{
	if (initialized) mm.printDetails();
	else MS.print("printDetails: MSCode is not initialized");
}

public void printData () 
{
	if (initialized) mm.dataPrint();
	else MS.print("printDetails: MSCode is not initialized");
}

public void dataOutputPrint (MSOutputPrint output) 
{
	mm.globals.printData(output,0,"");
	output.close();
}

public ByteCode compile (MSInputStream input) 
{
	reset();
	
	TokenTree tree = Parser.Parse (input);
	Semantics semantics = new Semantics(tree);
	common.initialize(semantics);
	semantics.analyze(tree);
	ByteCode bc = Generator.generate (tree, semantics, common);
	tree = null;	
	
	initialized = true;
	return bc;
}

public void run () 
{
	MS.assertion(mm != null,MC.EC_INTERNAL, "not initialized");
	mm.callFunction(0);
}

public void step () 
{
	MS.assertion(false,MC.EC_INTERNAL, "TODO");
}

public void compileAndRun (string s) 
{
	reset();
	MSInputArray ia = new MSInputArray(s);
	ByteCode bc = compile(ia);
	ia = null;
	mm = new MeanMachine(bc);
	run();
}

//public void compileAndRun (byte[] script) 
//{
//	compile(script);
//	run();
//}


}
}
