namespace Meanscript
{
	public class Common : MC
	{
		internal bool initialized = false;
		internal int callbackCounter;
		internal MCallback[] callbacks;
		internal System.Collections.Generic.Dictionary<MSText, int> callbackIDs = new System.Collections.Generic.Dictionary<MSText, int>(MS.textComparer);

		// private static data. common for all MeanScript objects

		public void PrintCallbacks()
		{
			MS.Verbose("-------- CALLBACKS:");
			for (int i = 0; i < MS.globalConfig.maxCallbacks; i++)
			{
				if (callbacks[i] != null) MS.Print("" + i + "");
			}
			MS.Verbose("");
		}

		private static void TrueCallback(MeanMachine mm, MArgs args)
		{
			mm.CallbackReturn(MS_TYPE_BOOL, 1);
		}
		private static void FalseCallback(MeanMachine mm, MArgs args)
		{
			mm.CallbackReturn(MS_TYPE_BOOL, 0);
		}
		private static void SumCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// SUM ////////////////");
			mm.CallbackReturn(MS_TYPE_INT, mm.stack[args.baseIndex] + mm.stack[args.baseIndex + 1]);
		}
		private static void EqCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// EQ ////////////////");

			MS.Verbose("compare " + mm.stack[args.baseIndex] + " and " + mm.stack[args.baseIndex + 1]);
			bool result = (mm.stack[args.baseIndex] == mm.stack[args.baseIndex + 1]);
			MS.Verbose("result: " + (result ? "true" : "false"));
			mm.CallbackReturn(MS_TYPE_INT, result ? 1 : 0);
		}
		private static void IfCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// IF ////////////////");

			if (mm.stack[args.baseIndex] != 0)
			{
				MS.Verbose("do it!");
				mm.Gosub(mm.stack[args.baseIndex + 1]);
			}
			else MS.Verbose("don't do!");
		}
		private static void SubCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// SUBTRACTION ////////////////");
			int a = mm.stack[args.baseIndex];
			int b = mm.stack[args.baseIndex + 1];
			MS.Verbose("calculate " + a + " - " + b + " = " + (a - b));
			mm.CallbackReturn(MS_TYPE_INT, a - b);
		}

		private static void printIntCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// PRINT ////////////////");
			MS.userOut.Print(mm.stack[args.baseIndex]).EndLine();
		}

		private static void PrintTextCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// PRINT TEXT ////////////////");

			int address = mm.texts[mm.stack[args.baseIndex]];
			int numChars = mm.GetStructCode()[address + 1];
			MS.userOut.Print("").PrintIntsToChars(mm.GetStructCode(), address + 2, numChars, false).EndLine();
		}

		private static void PrintCharsCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// PRINT CHARS  ////////////////");
			int numChars = mm.stack[args.baseIndex];
			MS.userOut.Print("").PrintIntsToChars(mm.stack, args.baseIndex + 1, numChars, false);
		}

		private static void PrintFloatCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// PRINT FLOAT ////////////////");
			MS.userOut.Print(MS.IntFormatToFloat(mm.stack[args.baseIndex]));
		}

		public int CreateCallback(MSText name, MS.MCallbackAction func, int returnType, StructDef argStruct)
		{
			MS.Verbose("Add callback: " + name);

			MCallback cb = new MCallback(func, returnType, argStruct);
			callbacks[callbackCounter] = cb;
			callbackIDs[name] = callbackCounter;
			return callbackCounter++;
		}

		public void Initialize(Semantics sem)
		{
			sem.AddElementaryType(MS_TYPE_INT, 1);
			sem.AddElementaryType(MS_TYPE_INT64, 2);
			sem.AddElementaryType(MS_TYPE_FLOAT, 1);
			sem.AddElementaryType(MS_TYPE_FLOAT64, 2);
			sem.AddElementaryType(MS_TYPE_TEXT, 1);
			sem.AddElementaryType(MS_TYPE_BOOL, 1);

			sem.AddElementaryType(MS_GEN_TYPE_ARRAY, -1); // generic type
			sem.AddElementaryType(MS_GEN_TYPE_CHARS, -1); // generic type

			CreateCallbacks(sem);
		}

		public void CreateCallbacks(Semantics sem)
		{

			// add return value and parameter struct def.
			StructDef trueArgs = new StructDef(sem, -1, callbackCounter);
			CreateCallback(new MSText("true"), (MeanMachine mm, MArgs args) => { TrueCallback(mm, args); }, MS_TYPE_BOOL, trueArgs);

			StructDef falseArgs = new StructDef(sem, -1, callbackCounter);
			CreateCallback(new MSText("false"), (MeanMachine mm, MArgs args) => { FalseCallback(mm, args); }, MS_TYPE_BOOL, falseArgs);

			StructDef sumArgs = new StructDef(sem, -1, callbackCounter);
			sumArgs.AddMember(MS_TYPE_INT, 1);
			sumArgs.AddMember(MS_TYPE_INT, 1);
			CreateCallback(new MSText("sum"), (MeanMachine mm, MArgs args) => { SumCallback(mm, args); }, MS_TYPE_INT, sumArgs);

			StructDef subArgs = new StructDef(sem, -1, callbackCounter);
			subArgs.AddMember(MS_TYPE_INT, 1);
			subArgs.AddMember(MS_TYPE_INT, 1);
			CreateCallback(new MSText("sub"), (MeanMachine mm, MArgs args) => { SubCallback(mm, args); }, MS_TYPE_INT, subArgs);

			StructDef ifArgs = new StructDef(sem, -1, callbackCounter);
			ifArgs.AddMember(MS_TYPE_BOOL, 1);
			ifArgs.AddMember(MS_TYPE_CODE_ADDRESS, 1);
			CreateCallback(new MSText("if"), (MeanMachine mm, MArgs args) => { IfCallback(mm, args); }, MS_TYPE_VOID, ifArgs);

			StructDef eqArgs = new StructDef(sem, -1, callbackCounter);
			eqArgs.AddMember(MS_TYPE_INT, 1);
			eqArgs.AddMember(MS_TYPE_INT, 1);
			CreateCallback(new MSText("eq"), (MeanMachine mm, MArgs args) => { EqCallback(mm, args); }, MS_TYPE_BOOL, eqArgs);

			StructDef printArgs = new StructDef(sem, -1, callbackCounter);
			printArgs.AddMember(MS_TYPE_INT, 1);
			CreateCallback(new MSText("print"), (MeanMachine mm, MArgs args) => { printIntCallback(mm, args); }, MS_TYPE_VOID, printArgs);

			StructDef textPrintArgs = new StructDef(sem, -1, callbackCounter);
			textPrintArgs.AddMember(MS_TYPE_TEXT, 1);
			CreateCallback(new MSText("prints"), (MeanMachine mm, MArgs args) => { PrintTextCallback(mm, args); }, MS_TYPE_VOID, textPrintArgs);

			StructDef floatPrintArgs = new StructDef(sem, -1, callbackCounter);
			floatPrintArgs.AddMember(MS_TYPE_FLOAT, 1);
			CreateCallback(new MSText("printf"), (MeanMachine mm, MArgs args) => { PrintFloatCallback(mm, args); }, MS_TYPE_VOID, floatPrintArgs);

			StructDef charsPrintArgs = new StructDef(sem, -1, callbackCounter);
			charsPrintArgs.AddMember(MS_GEN_TYPE_CHARS, 1);
			CreateCallback(new MSText("printc"), (MeanMachine mm, MArgs args) => { PrintCharsCallback(mm, args); }, MS_TYPE_VOID, charsPrintArgs);
		}

		public Common()
		{
			callbackCounter = MAX_MS_TYPES;
			callbacks = new MCallback[MS.globalConfig.maxCallbacks];
			for (int i = 0; i < MS.globalConfig.maxCallbacks; i++)
			{
				callbacks[i] = null;
			}
		}
		//
	}
}
