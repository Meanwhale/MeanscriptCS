using System;
using System.Collections.Generic;

namespace Meanscript
{
	public class Common : MC
	{
		internal bool initialized = false;
		internal Dictionary<int,CallbackType> callbacks = new Dictionary<int, CallbackType>();
		
		internal CallNameType
			genericGetAtCallName,
			genericSetAtCallName;
			
		public TypeDef VoidType		{ get; private set; }
		public TypeDef IntType		{ get; private set; }
		public TypeDef Int64Type	{ get; private set; }
		public TypeDef FloatType	{ get; private set; }
		public TypeDef Float64Type	{ get; private set; }
		public TypeDef TextType		{ get; private set; } 
		public TypeDef BoolType		{ get; private set; }
		
		public TypeDef PlusOperatorType		{ get; private set; }
		public TypeDef MinusOperatorType	{ get; private set; }
		public TypeDef DivOperatorType		{ get; private set; }
		public TypeDef MulOperatorType		{ get; private set; }

		// private static data. common for all MeanScript objects


		internal CallbackType FindCallback(MList<ArgType> args)
		{
			foreach (var cb in callbacks.Values)
			{
				if (cb.argStruct.Match(args)) return cb;
			}
			return null;
		}
		
		public void PrintCallbacks()
		{
			MS.Verbose("-------- CALLBACKS:");
			foreach (var cb in callbacks.Values)
			{
				MS.Print("callback id: " + cb.ID);
			}
			MS.Verbose("");
		}

		private static void BoolValueCallback(MeanMachine mm, MArgs args, bool b)
		{
			mm.CallbackReturn(MS_TYPE_BOOL, b ? 1 : 0);
		}
		private static void SumCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// SUM ////////////////");
			mm.CallbackReturn(MS_TYPE_INT, mm.stack[args.baseIndex] + mm.stack[args.baseIndex + 1]);
		}
		private static void Sum2Callback(MeanMachine mm, MArgs args)
		{
			int result = mm.stack[args.baseIndex] + mm.stack[args.baseIndex + 1];
			MS.Verbose("//////////////// SUM2="+result+" ////////////////");
			mm.CallbackReturn(MS_TYPE_INT, result);
		}
		private static void Sum3Callback(MeanMachine mm, MArgs args)
		{
			int result = mm.stack[args.baseIndex] + mm.stack[args.baseIndex + 1] + mm.stack[args.baseIndex + 2];
			MS.Verbose("//////////////// SUM3="+result+" ////////////////");
			mm.CallbackReturn(MS_TYPE_INT, result);
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

		private static void PrintIntCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// PRINT ////////////////");
			MS.userOut.Print(mm.stack[args.baseIndex]).EndLine();
		}

		private static void PrintTextCallback(MeanMachine mm, MArgs args)
		{
			//MS.Verbose("//////////////// PRINT TEXT ////////////////");

			//int address = mm.texts[mm.stack[args.baseIndex]];
			//int numChars = mm.GetStructCode()[address + 1];
			//MS.userOut.Print("").PrintIntsToChars(mm.GetStructCode(), address + 2, numChars, false).EndLine();
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

		//public int CreateCallback(Semantics sem, int nameID, MS.MCallbackAction func, int returnType, StructDef argStruct)
		//{
		//	MS.Verbose("Add callback: " + sem.tree.GetTextByID(nameID));
		//
		//	MCallback cb = new MCallback(func, returnType, argStruct);
		//	callbacks[sem.GetNewTypeID()] = cb;
		//}

		public void Initialize(Types types)
		{
			VoidType = types.AddElementaryType(MS_TYPE_VOID, 0);
			IntType = types.AddElementaryType(MS_TYPE_INT, 1);
			Int64Type = types.AddElementaryType(MS_TYPE_INT64, 2);
			FloatType = types.AddElementaryType(MS_TYPE_FLOAT, 1);
			Float64Type = types.AddElementaryType(MS_TYPE_FLOAT64, 2);
			TextType = types.AddElementaryType(MS_TYPE_TEXT, 1);
			BoolType = types.AddElementaryType(MS_TYPE_BOOL, 1);
			
			PlusOperatorType = types.AddOperatorType(MS_TYPE_PLUS, new MSText("+"));
			MinusOperatorType = types.AddOperatorType(MS_TYPE_MINUS, new MSText("-"));
			DivOperatorType = types.AddOperatorType(MS_TYPE_DIV, new MSText("/"));
			MulOperatorType = types.AddOperatorType(MS_TYPE_MUL, new MSText("*"));

			types.AddTypeDef(new NullType(MS_TYPE_NULL));

			genericGetAtCallName = new CallNameType(MS_TYPE_GET, new MSText("get"));
			genericSetAtCallName = new CallNameType(MS_TYPE_SET, new MSText("set"));
			
			types.AddTypeDef(genericGetAtCallName);
			types.AddTypeDef(genericSetAtCallName);

			CreateCallbacks(types);
		}

		public void CreateCallback(int id, Types types, ArgType returnType, ArgType [] args, MS.MCallbackAction _func)
		{
			var sd = new StructDef(types, 0);
			foreach(var arg in args) sd.AddMember(arg);
			var cbTypeDef = 
				new CallbackType(
					id,
					returnType,
					sd,
					_func
			);
			
			types.AddTypeDef(cbTypeDef);
			callbacks[id] = cbTypeDef;
		}

		public int CreateCallback(Types types, ArgType returnType, ArgType [] args, MS.MCallbackAction _func)
		{
			int cbTypeID = types.GetNewTypeID();
			CreateCallback(cbTypeID, types, returnType, args, _func);
			return cbTypeID;
		}

		public void CreateCallbacks(Types types)
		{
			var sumNameType = new CallNameType(types.GetNewTypeID(), new MSText("sum"));
			types.AddTypeDef(sumNameType);
			CreateCallback(types, ArgType.Data(IntType), new ArgType []{ ArgType.Void(sumNameType), ArgType.Data(IntType), ArgType.Data(IntType) }, (MeanMachine mm, MArgs a) => { Sum2Callback(mm, a); });
			CreateCallback(types, ArgType.Data(IntType), new ArgType []{ ArgType.Void(sumNameType), ArgType.Data(IntType), ArgType.Data(IntType), ArgType.Data(IntType) }, (MeanMachine mm, MArgs a) => { Sum3Callback(mm, a); });
			
			// sum by +
			CreateCallback(types, ArgType.Data(IntType), new ArgType []{ ArgType.Data(IntType), ArgType.Void(PlusOperatorType), ArgType.Data(IntType) }, (MeanMachine mm, MArgs a) => { Sum2Callback(mm, a); });
			
			var printNameType = new CallNameType(types.GetNewTypeID(), new MSText("print"));
			types.AddTypeDef(printNameType);
			CreateCallback(types, ArgType.Void(VoidType), new ArgType []{ ArgType.Void(printNameType), ArgType.Data(IntType) }, (MeanMachine mm, MArgs a) => { PrintIntCallback(mm, a); });

			var trueNameType = new CallNameType(types.GetNewTypeID(), new MSText("true"));
			types.AddTypeDef(trueNameType);
			CreateCallback(types, ArgType.Data(BoolType), new ArgType []{ ArgType.Void(trueNameType) }, (MeanMachine mm, MArgs a) => { BoolValueCallback(mm, a, true); });
			
			var falseNameType = new CallNameType(types.GetNewTypeID(), new MSText("false"));
			types.AddTypeDef(falseNameType);
			CreateCallback(types, ArgType.Data(BoolType), new ArgType []{ ArgType.Void(falseNameType) }, (MeanMachine mm, MArgs a) => { BoolValueCallback(mm, a, false); });

			//CreateCallback(sem, sumNameID, , MS_TYPE_INT, sd);
			/*
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
			*/
		}

	}
}
