namespace Meanscript.Core
{
	public enum Arg
	{
		VOID,
		DATA,		// push copy of data to the stack, sizeof = variable size
		ADDRESS,	// push address             -''-          = 1
					// masks: heap ID 0xffff0000, offset 0x0000ffff
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
		
		public static void PrintList(MList<ArgType> list, MSOutputPrint o)
		{
			foreach(var m in list)
				o.Print("<").Print(m.Ref == Arg.ADDRESS ? "#" : "").Print(":").Print(m.Def.TypeNameString()).Print(">");
		}
	}

	public abstract class TypeDef
	{
		public readonly int ID;
		public TypeDef(int id) { ID = id; }
		public abstract int SizeOf(); // size in stack. can be 0.
		public abstract MSText TypeName();
		public virtual string TypeNameString() { return TypeName() == null ? GetType().ToString() : TypeName().ToString();}
		public virtual void Init(CodeTypes sem) {}
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
	public class ScriptedFunctionNameType : TypeDef
	{
		// for code generation. no need to be saved to bytecode.
		public MSText Name { get; }
		public Context FuncContext { get; private set; }

		public ScriptedFunctionNameType(int id, MSText name, Context funcContext) : base(id)
		{
			Name = name;
			FuncContext = funcContext;
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
		internal readonly int argsSize;
		public CallbackType(int id, ArgType _returnType, StructDef _argStruct, MS.MCallbackAction _func) : base(id)
		{
			returnType = _returnType;
			argStruct = _argStruct;
			argsSize = _argStruct.StructSize();
			func = _func;
		}
		public CallbackType(int id, ArgType _returnType, int _argsSize, MS.MCallbackAction _func) : base(id)
		{
			returnType = _returnType;
			argStruct = null;
			argsSize = _argsSize;
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
			if (argStruct == null) return "internal callback, returns " + returnType.Def;
			return "callback " + argStruct + ", returns " + returnType.Def + ", args. size: " + argStruct.StructSize();
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
		private string typeName;
		private MSText textName;
		public PrimitiveType(int id, int size, string _name) : base(id)
		{
			_size=size;
			typeName = _name;
			textName = new MSText(_name);
		}

		public override bool TypeMatch(MList<ArgType> args)
		{
			return args.Size() == 1 && args.First().Def.ID == ID;
		}

		public override int SizeOf() { return _size; }
		public override MSText TypeName() { return textName; }
		public override string ToString() { return typeName; }
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
}