namespace Meanscript {

public class Common : MC {
internal bool initialized = false;
internal int callbackCounter;
internal MCallback [] callbacks;
internal System.Collections.Generic.Dictionary<MSText, int> callbackIDs = new System.Collections.Generic.Dictionary<MSText, int>(MS.textComparer);

// private static data. common for all MeanScript objects

public void printCallbacks () 
{
	MS.verbose("-------- CALLBACKS:");
	for (int i=0; i < MS.globalConfig.maxCallbacks; i++)
	{
		if (callbacks[i] != null) MS.print("" + i + "");
	}
	MS.verbose("");
}

private static void trueCallback(MeanMachine mm, MArgs args) 
{
	mm.callbackReturn(MS_TYPE_BOOL, 1);
}
private static void falseCallback(MeanMachine mm, MArgs args) 
{
	mm.callbackReturn(MS_TYPE_BOOL, 0);
}
private static void sumCallback(MeanMachine mm, MArgs args) 
{
	MS.verbose("//////////////// SUM ////////////////");
	mm.callbackReturn(MS_TYPE_INT, mm.stack[args.baseIndex] + mm.stack[args.baseIndex+1]);
}
private static void eqCallback(MeanMachine mm, MArgs args) 
{
	MS.verbose("//////////////// EQ ////////////////");
	
	MS.verbose("compare " + mm.stack[args.baseIndex] + " and " + mm.stack[args.baseIndex+1]);
	bool result = (mm.stack[args.baseIndex] == mm.stack[args.baseIndex+1]);
	MS.verbose("result: " + (result ? "true" : "false"));
	mm.callbackReturn(MS_TYPE_INT, result ? 1 : 0);
}
private static void ifCallback(MeanMachine mm, MArgs args) 
{
	MS.verbose("//////////////// IF ////////////////");
	
	if (mm.stack[args.baseIndex] != 0) {	
		MS.verbose("do it!");	
		mm.gosub(mm.stack[args.baseIndex+1]);
	} else MS.verbose("don't do!");
}
private static void subCallback(MeanMachine mm, MArgs args) 
{
	MS.verbose("//////////////// SUBTRACTION ////////////////");
	int a = mm.stack[args.baseIndex];
	int b = mm.stack[args.baseIndex+1];
	MS.verbose("calculate " + a + " - " + b + " = " + (a-b));
	mm.callbackReturn(MS_TYPE_INT, a - b);
}

private static void printIntCallback(MeanMachine mm, MArgs args) 
{
	MS.verbose("//////////////// PRINT ////////////////");
	MS.userOut.print(mm.stack[args.baseIndex]).endLine();
}

private static void printTextCallback(MeanMachine mm, MArgs args) 
{
	MS.verbose("//////////////// PRINT TEXT ////////////////");
	
	int address = mm.texts[mm.stack[args.baseIndex]];
	int numChars = mm.getStructCode()[address + 1];
	MS.userOut.print("").printIntsToChars(mm.getStructCode(), address + 2, numChars, false).endLine();
}

private static void printCharsCallback(MeanMachine mm, MArgs args) 
{
	MS.verbose("//////////////// PRINT CHARS  ////////////////");
	int numChars = mm.stack[args.baseIndex];
	MS.userOut.print("").printIntsToChars(mm.stack, args.baseIndex + 1, numChars, false);
}

private static void printFloatCallback(MeanMachine mm, MArgs args) 
{
	MS.verbose("//////////////// PRINT FLOAT ////////////////");
	MS.userOut.print(MS.intFormatToFloat(mm.stack[args.baseIndex]));
}

public int  createCallback (MSText name, MS.MCallbackAction func, int returnType, StructDef argStruct) 
{
	MS.verbose("Add callback: " + name);
	
	MCallback cb = new MCallback(func, returnType, argStruct);
	callbacks[callbackCounter] = cb;
	callbackIDs[ name] =  callbackCounter;
	return callbackCounter++;
}

public void initialize (Semantics sem) 
{
	sem.addElementaryType(MS_TYPE_INT,     1);
	sem.addElementaryType(MS_TYPE_INT64,   2);
	sem.addElementaryType(MS_TYPE_FLOAT,   1);
	sem.addElementaryType(MS_TYPE_FLOAT64, 2);
	sem.addElementaryType(MS_TYPE_TEXT,    1);
	sem.addElementaryType(MS_TYPE_BOOL,    1);
	
	sem.addElementaryType(MS_GEN_TYPE_ARRAY,  -1); // generic type
	sem.addElementaryType(MS_GEN_TYPE_CHARS,  -1); // generic type
	
	createCallbacks(sem);
}

public void createCallbacks (Semantics sem) 
{
	
	// add return value and parameter struct def.
	StructDef trueArgs = new StructDef(sem, -1, callbackCounter);
	createCallback(new MSText("true"), (MeanMachine mm, MArgs args) => {trueCallback(mm,args);}, MS_TYPE_BOOL, trueArgs);
	
	StructDef falseArgs = new StructDef(sem, -1, callbackCounter);
	createCallback(new MSText("false"), (MeanMachine mm, MArgs args) => {falseCallback(mm,args);}, MS_TYPE_BOOL, falseArgs);
	
	StructDef sumArgs = new StructDef(sem, -1, callbackCounter);
	sumArgs.addMember(MS_TYPE_INT, 1);
	sumArgs.addMember(MS_TYPE_INT, 1);
	createCallback(new MSText("sum"), (MeanMachine mm, MArgs args) => {sumCallback(mm,args);}, MS_TYPE_INT, sumArgs);
	
	StructDef subArgs = new StructDef(sem, -1, callbackCounter);
	subArgs.addMember(MS_TYPE_INT, 1);
	subArgs.addMember(MS_TYPE_INT, 1);
	createCallback(new MSText("sub"), (MeanMachine mm, MArgs args) => {subCallback(mm,args);}, MS_TYPE_INT, subArgs);
	
	StructDef ifArgs = new StructDef(sem, -1, callbackCounter);
	ifArgs.addMember(MS_TYPE_BOOL, 1);
	ifArgs.addMember(MS_TYPE_CODE_ADDRESS, 1);
	createCallback(new MSText("if"), (MeanMachine mm, MArgs args) => {ifCallback(mm,args);}, MS_TYPE_VOID, ifArgs);
	
	StructDef eqArgs = new StructDef(sem, -1, callbackCounter);
	eqArgs.addMember(MS_TYPE_INT, 1);
	eqArgs.addMember(MS_TYPE_INT, 1);
	createCallback(new MSText("eq"), (MeanMachine mm, MArgs args) => {eqCallback(mm,args);}, MS_TYPE_BOOL, eqArgs);
	
	StructDef printArgs = new StructDef(sem, -1, callbackCounter);
	printArgs.addMember(MS_TYPE_INT, 1);
	createCallback(new MSText("print"), (MeanMachine mm, MArgs args) => {printIntCallback(mm,args);}, MS_TYPE_VOID, printArgs);
	
	StructDef textPrintArgs = new StructDef(sem, -1, callbackCounter);
	textPrintArgs.addMember(MS_TYPE_TEXT, 1);
	createCallback(new MSText("prints"), (MeanMachine mm, MArgs args) => {printTextCallback(mm,args);}, MS_TYPE_VOID, textPrintArgs);
	
	StructDef floatPrintArgs = new StructDef(sem, -1, callbackCounter);
	floatPrintArgs.addMember(MS_TYPE_FLOAT, 1);
	createCallback(new MSText("printf"), (MeanMachine mm, MArgs args) => {printFloatCallback(mm,args);}, MS_TYPE_VOID, floatPrintArgs);
	
	StructDef charsPrintArgs = new StructDef(sem, -1, callbackCounter);
	charsPrintArgs.addMember(MS_GEN_TYPE_CHARS, 1);
	createCallback(new MSText("printc"), (MeanMachine mm, MArgs args) => {printCharsCallback(mm,args);}, MS_TYPE_VOID, charsPrintArgs);
}

public Common () 
{
	callbackCounter = MAX_MS_TYPES;
	callbacks = new MCallback[MS.globalConfig.maxCallbacks];
	for (int i=0; i < MS.globalConfig.maxCallbacks; i++)
	{
		callbacks[i] = null;
	}
}
//
}
}
