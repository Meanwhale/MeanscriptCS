using System;
using System.Collections.Generic;

namespace Meanscript.Core
{
	public class BasicTypes : ITypes
	{
		// basic types that are in common use.
		// this class need to be created only once.

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
		public TypeDef MapType		{ get; private set; }
		
		public TypeDef PlusOperatorType		{ get; private set; }
		public TypeDef MinusOperatorType	{ get; private set; }
		public TypeDef DivOperatorType		{ get; private set; }
		public TypeDef MulOperatorType		{ get; private set; }
		
		private static bool initialized = false;

		public BasicTypes()
		{
			MS.Assertion(!initialized);
			initialized = true;

			VoidType = AddElementaryType(MC.BASIC_TYPE_VOID, 0, "void");
			IntType = AddElementaryType(MC.BASIC_TYPE_INT, 1, "int");
			Int64Type = AddElementaryType(MC.BASIC_TYPE_INT64, 2, "int64");
			FloatType = AddElementaryType(MC.BASIC_TYPE_FLOAT, 1, "float");
			Float64Type = AddElementaryType(MC.BASIC_TYPE_FLOAT64, 2, "float64");
			TextType = AddElementaryType(MC.BASIC_TYPE_TEXT, 1, "text");
			BoolType = AddElementaryType(MC.BASIC_TYPE_BOOL, 1, "bool");
			MapType = AddElementaryType(MC.BASIC_TYPE_MAP, 1, "map");
			
			PlusOperatorType = AddOperatorType(MC.BASIC_TYPE_PLUS, new MSText("+"));
			MinusOperatorType = AddOperatorType(MC.BASIC_TYPE_MINUS, new MSText("-"));
			DivOperatorType = AddOperatorType(MC.BASIC_TYPE_DIV, new MSText("/"));
			MulOperatorType = AddOperatorType(MC.BASIC_TYPE_MUL, new MSText("*"));

			//AddBasicTypeDef(new NullType(MC.BASIC_TYPE_NULL));

			genericGetAtCallName = new CallNameType(MC.BASIC_TYPE_GET, new MSText("get"));
			genericSetAtCallName = new CallNameType(MC.BASIC_TYPE_SET, new MSText("set"));
			
			AddBasicTypeDef(genericGetAtCallName);
			AddBasicTypeDef(genericSetAtCallName);

			CreateCallbacks();
		}
		
		private int nextCallbackID = MC.FIRST_BASIC_CALLBACK;
		private int NewCallbackID()
		{
			MS.Assertion(nextCallbackID < MC.MAX_BASIC_TYPES);
			return nextCallbackID++;
		}

		public TypeDef AddElementaryType(int typeID, int size, string name)
		{
			return AddBasicTypeDef(new PrimitiveType(typeID, size, name));
		}
		public TypeDef AddOperatorType(int typeID, MSText name)
		{
			return AddBasicTypeDef(new OperatorType(typeID, name));
		}
		public TypeDef AddBasicTypeDef(TypeDef newType)
		{
			MS.Assertion(!types.ContainsKey(newType.ID));
			types[newType.ID] = newType;
			return newType;
		}
		
		public bool HasBasicType(int id)
		{
			return types.ContainsKey(id);
		}

		public TypeDef GetBasicType(int id, NodeIterator it = null)
		{
			if (types.ContainsKey(id)) return types[id];
			return null;
		}
		public TypeDef GetBasicType(MSText name, NodeIterator it = null)
		{
			foreach(var t in types.Values)
			{
				if (t is TypeDef d)
				{
					if (name.Equals(d.TypeName())) return d;
				}
			}
			return null;
		}
		
		public void PrintCallbacks()
		{
			MS.Verbose(MS.Title("CALLBACKS"));
			foreach (var cb in callbacks.Values)
			{
				MS.Print("callback id: " + cb.ID);
			}
			MS.Verbose("");
		}

		private static void BoolValueCallback(MeanMachine mm, MArgs args, bool b)
		{
			mm.CallbackReturn(MC.BASIC_TYPE_BOOL, b ? 1 : 0);
		}
		private static void SumCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose(MS.Title("SUM"));
			mm.CallbackReturn(MC.BASIC_TYPE_INT, mm.stack[args.baseIndex] + mm.stack[args.baseIndex + 1]);
		}
		private static void Sum2Callback(MeanMachine mm, MArgs args)
		{
			int result = mm.stack[args.baseIndex] + mm.stack[args.baseIndex + 1];
			MS.Verbose(MS.Title("SUM2="+result+""));
			mm.CallbackReturn(MC.BASIC_TYPE_INT, result);
		}
		private static void Sum3Callback(MeanMachine mm, MArgs args)
		{
			int result = mm.stack[args.baseIndex] + mm.stack[args.baseIndex + 1] + mm.stack[args.baseIndex + 2];
			MS.Verbose(MS.Title("SUM3="+result+""));
			mm.CallbackReturn(MC.BASIC_TYPE_INT, result);
		}
		private static void EqCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose(MS.Title("EQ"));

			MS.Verbose("compare " + mm.stack[args.baseIndex] + " and " + mm.stack[args.baseIndex + 1]);
			bool result = (mm.stack[args.baseIndex] == mm.stack[args.baseIndex + 1]);
			MS.Verbose("result: " + (result ? "true" : "false"));
			mm.CallbackReturn(MC.BASIC_TYPE_INT, result ? 1 : 0);
		}
		private static void IfCallback(MeanMachine mm, MArgs args)
		{
			//MS.Verbose(MS.Title("IF"));

			//if (mm.stack[args.baseIndex] != 0)
			//{
			//	MS.Verbose("do it!");
			//	mm.Gosub(mm.stack[args.baseIndex + 1]);
			//}
			//else MS.Verbose("don't do!");
		}
		private static void SubCallback(MeanMachine mm, MArgs args)
		{
			//MS.Verbose(MS.Title("SUBTRACTION"));
			//int a = mm.stack[args.baseIndex];
			//int b = mm.stack[args.baseIndex + 1];
			//MS.Verbose("calculate " + a + " - " + b + " = " + (a - b));
			//mm.CallbackReturn(MC.BASIC_TYPE_INT, a - b);
		}

		private static void PrintIntCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose(MS.Title("PRINT"));
			MS.userOut.Print(mm.stack[args.baseIndex]).EndLine();
		}

		private static void PrintTextCallback(MeanMachine mm, MArgs args)
		{
			//MS.Verbose(MS.Title("PRINT TEXT"));

			//int address = mm.texts[mm.stack[args.baseIndex]];
			//int numChars = mm.GetStructCode()[address + 1];
			//MS.userOut.Print("").PrintIntsToChars(mm.GetStructCode(), address + 2, numChars, false).EndLine();
		}

		private static void PrintCharsCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose(MS.Title("PRINT CHARS "));
			int numChars = mm.stack[args.baseIndex];
			MS.userOut.Print("").PrintIntsToChars(mm.stack.Data(), args.baseIndex + 1, numChars, false);
		}

		private static void PrintFloatCallback(MeanMachine mm, MArgs args)
		{
			MS.Verbose(MS.Title("PRINT FLOAT"));
			MS.userOut.Print(MS.IntFormatToFloat(mm.stack[args.baseIndex]));
		}

		public int CreateBasicCallback(ArgType returnType, ArgType [] args, MS.MCallbackAction _func)
		{
			int cbTypeID = NewCallbackID();
			CreateCallback(cbTypeID, returnType, args, _func);
			return cbTypeID;
		}

		public void CreateCallbacks()
		{
			var sumNameType = new CallNameType(NewCallbackID(), new MSText("sum"));
			AddBasicTypeDef(sumNameType);
			CreateBasicCallback(ArgType.Data(IntType), new ArgType []{ ArgType.Void(sumNameType), ArgType.Data(IntType), ArgType.Data(IntType) }, (MeanMachine mm, MArgs a) => { Sum2Callback(mm, a); });
			CreateBasicCallback(ArgType.Data(IntType), new ArgType []{ ArgType.Void(sumNameType), ArgType.Data(IntType), ArgType.Data(IntType), ArgType.Data(IntType) }, (MeanMachine mm, MArgs a) => { Sum3Callback(mm, a); });
			
			// sum by +
			CreateBasicCallback(ArgType.Data(IntType), new ArgType []{ ArgType.Data(IntType), ArgType.Void(PlusOperatorType), ArgType.Data(IntType) }, (MeanMachine mm, MArgs a) => { Sum2Callback(mm, a); });
			
			var printNameType = new CallNameType(NewCallbackID(), new MSText("print"));
			AddBasicTypeDef(printNameType);
			CreateBasicCallback(ArgType.Void(VoidType), new ArgType []{ ArgType.Void(printNameType), ArgType.Data(IntType) }, (MeanMachine mm, MArgs a) => { PrintIntCallback(mm, a); });

			var trueNameType = new CallNameType(NewCallbackID(), new MSText("true"));
			AddBasicTypeDef(trueNameType);
			CreateBasicCallback(ArgType.Data(BoolType), new ArgType []{ ArgType.Void(trueNameType) }, (MeanMachine mm, MArgs a) => { BoolValueCallback(mm, a, true); });
			
			var falseNameType = new CallNameType(NewCallbackID(), new MSText("false"));
			AddBasicTypeDef(falseNameType);
			CreateBasicCallback(ArgType.Data(BoolType), new ArgType []{ ArgType.Void(falseNameType) }, (MeanMachine mm, MArgs a) => { BoolValueCallback(mm, a, false); });
		}
	}
}
