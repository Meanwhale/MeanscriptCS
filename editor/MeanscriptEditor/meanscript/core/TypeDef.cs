using System;

namespace Meanscript
{
	public enum Arg
	{
		VOID,
		DATA,		// push copy of data to the stack, sizeof = variable size
		ADDRESS,	// push address             -''-          = 1
		DYNAMIC		// TODO
	}

	public class ArgType
	{
		// kuten esim. C:
		//		foo(int a)		// Ref = COPY
		//		foo(int * a)	// Ref = ADDRESS

		public Arg Ref;
		public TypeDef Def;

		public ArgType(Arg r, TypeDef d) { Ref = r; Def = d; }
		public static ArgType Void(TypeDef t)		{ return new ArgType(Arg.VOID, t); }
		public static ArgType Data(TypeDef t)		{ return new ArgType(Arg.DATA, t); }
		public static ArgType Addr(TypeDef t)		{ return new ArgType(Arg.ADDRESS, t); }
		public static ArgType Dynamic(TypeDef t)	{ return new ArgType(Arg.DYNAMIC, t); }
		
		public static void PrintList(MList<ArgType> list, MSOutputPrint o)
		{
			foreach(var m in list)
				o.Print("<").Print(m.Ref.ToString()).Print(":").Print(m.Def.TypeNameString()).Print(">");
		}
	}

	public abstract class TypeDef
	{
		public readonly int ID;
		public TypeDef(int id) { ID = id; }
		public abstract int SizeOf(); // size in stack. can be 0.
		public abstract MSText TypeName();
		public virtual string TypeNameString() { return TypeName().ToString();}
	}
	public class OperatorType : TypeDef
	{
		public MSText Name { get; }
		public OperatorType(int id, MSText name) : base(id) { Name = name; }
		public override int SizeOf() { return 0; }
		public override MSText TypeName() { return Name; }
	}
	public class CallNameType : TypeDef
	{
		public MSText Name { get; }
		public CallNameType(int id, MSText name) : base(id)
		{
			Name = name;
		}
		public override int SizeOf() { return 0; }

		public override MSText TypeName()
		{
			return Name;
		}
		public override string ToString()
		{
			return Name.ToString();
		}
	}
	public class CallbackType : TypeDef
	{
		// "type" that defines a list of arguments. list can consist of other types, and must include callname or operator
		internal readonly MS.MCallbackAction func;
		internal readonly ArgType returnType;
		internal readonly StructDef argStruct;
		public CallbackType(int id, ArgType _returnType, StructDef _argStruct, MS.MCallbackAction _func) : base(id)
		{
			returnType = _returnType;
			argStruct = _argStruct;
			func = _func;
		}
		public override int SizeOf() { return 0; }

		public override MSText TypeName()
		{
			return null; // CallbackType is nameless, it's indentified by its argument list
		}
		internal void Print(MSOutputPrint printOut)
		{
			argStruct.PrintArgTypes(printOut);
		}
		public override string ToString()
		{
			return "// callback: " + argStruct + ", returns " + returnType.Def;
		}
	}
	public abstract class DataTypeDef : TypeDef
	{
		// variable or struct types like "int" or user-defined "vec2"
		public DataTypeDef(int id) : base(id) {	}

		public abstract bool TypeMatch(MList<ArgType> args);
	}
	public class PrimitiveType : DataTypeDef
	{
		private int _size;
		public PrimitiveType(int id, int size) : base(id) { _size=size; }

		public override bool TypeMatch(MList<ArgType> args)
		{
			return args.Size() == 1 && args.First().Def.ID == ID;
		}

		public override int SizeOf() { return _size; }
		public override MSText TypeName() { return MC.primitiveNames[ID]; }
		public override string ToString()
		{
			return MC.primitiveNames[ID].ToString();
		}
	}
	public class StructDefType : DataTypeDef
	{
		public readonly MSText Name;
		public readonly StructDef SD;
		public StructDefType(int id, MSText name, StructDef sd) : base(id)
		{
			Name = name;
			SD = sd;
		}

		public override int SizeOf()
		{
			return SD.StructSize();
		}

		public override MSText TypeName()
		{
			return Name;
		}
		public override bool TypeMatch(MList<ArgType> args)
		{
			return SD.Match(args);
		}
		public override string ToString()
		{
			return "struct " + Name + ": " + SD.ToString();
		}
	}
	public abstract class GenericType : DataTypeDef
	{
		private readonly MSText _name;

		public GenericType(int id, MSText name) : base(id)
		{
			_name = name;
		}
		public override MSText TypeName()
		{
			return _name;
		}
	}

	public abstract class DynamicType : GenericType
	{
		protected DynamicType(int id, MSText name) : base(id, name) { }
	}

	public class NullType : DynamicType
	{
		public NullType(int id) : base(id, new MSText("null")) { }
		public override int SizeOf() { return 1; }
		public override bool TypeMatch(MList<ArgType> args)
		{
			return args.Size() == 1 && args.First().Ref == Arg.DYNAMIC && (args.First().Def is DynamicType);
		}
	}

	public class PointerType : DynamicType
	{
		public readonly DataTypeDef itemType;
		public int ItemSize { get { return itemType.SizeOf(); } }
		public readonly int SetterID;
		public PointerType(int id, MList<MNode> genArgs, Semantics sem, Common common, NodeIterator it) : base(id, null)
		{
			MS.SyntaxAssertion(genArgs.Size() == 1, it, "pointer: 1 argument expected");
			itemType = sem.GetDataType(genArgs.First().data);

			var thisType = sem.AddTypeDef(this);			// create a callback that generator finds with certain arguments "thisKindOfArray @getAt index"
			SetterID = common.CreateCallback(
				sem,
				ArgType.Void(common.VoidType),					// return value
				new ArgType [] {
					ArgType.Void(common.genericSetAtCallName),	// "get"
					ArgType.Data(thisType),						// "thisKindOfArray"
					ArgType.Data(itemType) },					// data type
				Setter											// callback to call when executing the code
			);
		}

		private void Setter(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// pointer setter: base " + args.baseIndex);

			// stack: top >>      data      >> tag address >> ...
			
			// TODO: we can be in stack OR DYNAMIC OBJECT

			var offset = mm.stack[mm.stackTop - ItemSize - 1];
			var address = mm.stack[mm.stackBase];
			if (MS._verboseOn) MS.printOut.Print("previous tag address: ").Print(address).Print("+").Print(offset).Print(", tag: ").PrintHex(mm.stack[address]).EndLine();

			// TODO: don't free! overwrite old DData

			// mm.Heap.Free(mm.stack[address + offset], itemType.ID);

			var data = new IntArray(ItemSize);
			IntArray.Copy(mm.stack, mm.stackTop - ItemSize, data, 0, itemType.SizeOf());
			int tag = mm.Heap.Alloc(itemType.ID, data);
			//mm.stackTop -= ItemSize + 1; // args size is item size + address size (1)
			mm.CallbackReturn(ID, tag);
		}

		public override int SizeOf()
		{
			return 1; // pointer to the dynamic object array
		}

		public override bool TypeMatch(MList<ArgType> args)
		{
			return args.Size() == 1 && args.First().Def.ID == ID;
		}
		public override string TypeNameString()
		{
			return "ptr";
		}
		public override string ToString()
		{
			return "ptr [" + itemType + "]";
		}
	}

	public class GenericArrayType : GenericType
	{
		// generic type of array. size is defined compile time for fixed size total data.
		// TODO: dynamic array with dynamic objects.

		public readonly DataTypeDef itemType;
		public readonly int itemCount;
		public GenericArrayType(int id, MList<MNode> genArgs, Semantics sem, Common common, NodeIterator it) : base(id, null)
		{
			MS.SyntaxAssertion(genArgs.Size() == 2, it, "array: 2 arguments expected");
			itemType = sem.GetDataType(genArgs.First().data);
			MS.Assertion(itemType != null, MC.EC_SCRIPT, "generic array: type expected");
			itemCount = MS.ParseInt(genArgs.Last().data.GetString());
			MS.Assertion(itemCount > 0 && itemCount < 64000, MC.EC_SCRIPT, "generic array: unacceptable item count: " + itemCount);
			
			var thisType = sem.AddTypeDef(this);

			// create a callback that generator finds with certain arguments "thisKindOfArray @getAt index"
			common.CreateCallback(
				sem,
				ArgType.Data(itemType),						// return value
				new ArgType [] {
					ArgType.Addr(thisType),						// "thisKindOfArray"
					ArgType.Void(common.genericGetAtCallName),	// "get"
					ArgType.Data(common.IntType) },				// index type
				Getter								// callback to call when executing the code
			);
		}

		private void Getter(MeanMachine mm, MArgs args)
		{
			MS.Verbose("GenericArrayType.Getter");
			// read args from stack and push item address
			int arrayAddress = mm.stack[args.baseIndex];
			int index = mm.stack[args.baseIndex + 1];
			MS.Assertion(index >= 0 && index < itemCount, MC.EC_CODE, "index out of bounds: " + index + "/" + itemCount);
			mm.CallbackReturn(itemType.ID, arrayAddress + (index * itemType.SizeOf()));
		}

		public override bool TypeMatch(MList<ArgType> args)
		{
			if (args.Size() != itemCount) return false;
			foreach(var a in args) if (a.Def.ID != itemType.ID) return false;
			return true;
		}
		public override int SizeOf()
		{
			return itemCount * itemType.SizeOf();
		}

		internal bool ValidIndex(MList<ArgType> args)
		{
			return args.Size() == 1 && args.First().Def.ID == MC.MS_TYPE_INT;
		}
		public override string TypeNameString()
		{
			return "array";
		}
		public override string ToString()
		{
			return "array [" + itemType + ", " + itemCount + "]";
		}
	}
	public class GenericCharsType : GenericType
	{
		public readonly int maxChars;
		private int size;

		public GenericCharsType(int id, MList<MNode> genArgs, Semantics sem, Common common, NodeIterator it) : base(id, null)
		{
			MS.SyntaxAssertion(genArgs.Size() == 1, it, "chars: 1 arguments expected");
			maxChars = MS.ParseInt(genArgs.Last().data.GetString());
			MS.Assertion(maxChars > 0 && maxChars < 64000, MC.EC_SCRIPT, "chars: unacceptable count: " + maxChars);
			
			sem.AddTypeDef(this);
			
			size = (maxChars / 4) + 2;
			// eg. "moi!" ->
			// 0: [4 = numChars]
			// 1: ['m''o''i''!']
			// 2: [    '\0'    ]
		}

		public override bool TypeMatch(MList<ArgType> args)
		{
			return args.Size() == 1 && args.First().Def.ID == MC.MS_TYPE_TEXT;
		}
		public override int SizeOf()
		{
			return size;
		}
		public override string TypeNameString()
		{
			return "chars";
		}
		public override string ToString()
		{
			return "chars [" + maxChars + "]";
		}
	}
}
