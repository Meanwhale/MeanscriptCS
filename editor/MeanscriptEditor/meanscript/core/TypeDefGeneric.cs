namespace Meanscript
{
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
		// TODO: poista jos turha
		public NullType(int id) : base(id, new MSText("null")) { }
		public override int SizeOf() { return 1; }
		public override bool TypeMatch(MList<ArgType> args)
		{
			return args.Size() == 1 && args.First().Ref == Arg.ADDRESS && (args.First().Def is DynamicType);
		}
	}

	public abstract class GenericFactory
	{
		// - create a generic type with type and other parameters (during semantic analysis)
		// - encode on bytecode generation (Generator)
		// - decode on bytecode initialization (MeanMachine)

		public abstract int CodeID();
		public abstract string TypeName();
		public abstract GenericType Create(int id, MList<MNode> genArgs, CodeTypes types, NodeIterator it);
		public abstract GenericType Decode(MeanMachine mm, int [] args);
		public abstract void Encode(ByteCode bc, GenericType t);
		public abstract int NumArgs();
		
		public static MList<GenericFactory> factories = new MList<GenericFactory>();

		public static GenericFactory Get(int codeID)
		{
			foreach(var f in factories) if (codeID == f.CodeID()) return f;
			MS.Assertion(false, MC.EC_INTERNAL, "no GenericFactory for codeID " + codeID);
			return null;
		}

		static GenericFactory()
		{
			factories.Add(new ObjectType.Factory());
			factories.Add(new GenericArrayType.Factory());
			factories.Add(new GenericCharsType.Factory());
		}
	}

	public class ObjectType : DynamicType
	{
		public class Factory : GenericFactory
		{
			public override int CodeID()
			{
				return MC.BASIC_TYPE_GENERIC_OBJECT;
			}
			public override GenericType Create(int id, MList<MNode> genArgs, CodeTypes types, NodeIterator it)
			{
				// create type on semantic construction
				MS.SyntaxAssertion(genArgs.Size() == 1, it, "object: 1 argument expected");
				var itemType = types.GetDataType(genArgs.First().data);
				MS.SyntaxAssertion(itemType != null, it, "in obj[x], x is of undefined type: " + genArgs.First().data);
				return new ObjectType(types, id, itemType, types.GetNewTypeID());
			}
			public override void Encode(ByteCode bc, GenericType t)
			{
				// data for bytecode
				var ot = (ObjectType)t;
				bc.AddInstruction(MC.OP_GENERIC_TYPE, 3, CodeID());
				bc.AddWord(ot.ID);
				bc.AddWord(ot.itemType.ID);
				bc.AddWord(ot.SetterID);
			}
			public override GenericType Decode(MeanMachine mm, int [] args)
			{
				var itemType = mm.codeTypes.GetDataType(args[1]);
				MS.Assertion(itemType != null, MC.EC_CODE, "ObjectType Factory Decode: object's item type not found, type ID " + args[1]);
				return new ObjectType(mm.codeTypes, args[0], itemType, args[2]);
			}
			public override string TypeName()
			{
				return "obj";
			}
			public override int NumArgs()
			{
				return 2;
			}
		}

		public readonly DataTypeDef itemType;
		public int ItemSize { get { return itemType.SizeOf(); } }
		public int SetterID { get; private set; }
		public ObjectType(CodeTypes types, int id, DataTypeDef _itemType, int setterID) : base(id, null)
		{
			itemType = _itemType;
			CreateSetter(setterID, types);
		}

		internal void CreateSetter(int id, CodeTypes types)
		{	
			// NOTE: generator handles the setter call. arg size must be right so that stack is popped for right amount after call

			SetterID = id;
			types.CreateCallback(
				SetterID,
				ArgType.Void(MC.basics.VoidType),	// return value.
				itemType.SizeOf(),					// data size
				Setter								// callback to call when executing the code
			);
		}

		public override void Init(CodeTypes types)
		{
			// HUOM!
			// obj:lle argStructin koko voi olla väärä jos obj tyyppi sama kuin parent structin...
			// MUTTA ei ole tällä hetkellä mahdollista. viitteet structista itseen tarvii isomman remontin.
			// lisätietoja TODO-docissa.

			//var cb = common.callbacks[SetterID];
			//cb.argStruct = new StructDef(types, -1);
			//cb.argStruct.AddMember(-1, ArgType.Void(this));
			//cb.argStruct.AddMember(-1, ArgType.Void(types.common.VoidType));
			//cb.argStruct.AddMember(-1, ArgType.Data(itemType));
		}

		private void Setter(MeanMachine mm, MArgs args)
		{
			MS.Verbose("//////////////// object setter: base " + args.baseIndex);

			// stack: top >>      data      >> address >> ...
			
			// TODO: onko mahdollista käyttää vanhaa DDataa jos sellainen on? sen osoite pitäisi saada jotenkin...
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
			MS.Assertion(false);
			return false;
			//return args.Size() == 1 && args.First().Def.ID == ID;
		}
		public override string TypeNameString()
		{
			return "obj";
		}
		public override string ToString()
		{
			return "obj [" + itemType + "]";
		}
	}

	public class GenericArrayType : GenericType
	{
		public class Factory : GenericFactory
		{
			public override int CodeID()
			{
				return MC.BASIC_TYPE_GENERIC_ARRAY;
			}
			public override GenericType Create(int id, MList<MNode> genArgs, CodeTypes types, NodeIterator it)
			{
				MS.SyntaxAssertion(genArgs.Size() == 2, it, "array: 2 arguments expected");
				var itemType = types.GetDataType(genArgs.First().data);
				MS.Assertion(itemType != null, MC.EC_SCRIPT, "generic array: type expected");
				var itemCount = MS.ParseInt(genArgs.Last().data.GetString());
				MS.Assertion(itemCount > 0 && itemCount < 64000, MC.EC_SCRIPT, "generic array: unacceptable item count: " + itemCount);

				return new GenericArrayType(types, id, itemType, itemCount, types.GetNewTypeID());
			}
			public override void Encode(ByteCode bc, GenericType t)
			{
				// data for bytecode
				var at = (GenericArrayType)t;
				bc.AddInstruction(MC.OP_GENERIC_TYPE, 4, CodeID());
				bc.AddWord(at.ID);
				bc.AddWord(at.itemType.ID);
				bc.AddWord(at.itemCount);
				bc.AddWord(at.accessorID);
			}
			public override GenericType Decode(MeanMachine mm, int [] args)
			{
				var itemType = mm.codeTypes.GetDataType(args[1]);
				MS.Assertion(itemType != null, MC.EC_CODE, "ObjectType Factory Decode: object's item type not found, type ID " + args[1]);
				return new GenericArrayType(mm.codeTypes,
					args[0],	// type ID
					itemType,	// item's type
					args[2],	// item count
					args[3]);	// accessor ID
			}
			public override string TypeName()
			{
				return "array";
			}
			public override int NumArgs()
			{
				return 4;
			}
		}
		// generic type of array. size is defined compile time for fixed size total data.
		// TODO: dynamic array with dynamic objects.

		public readonly DataTypeDef itemType;
		public readonly int itemCount, accessorID;
		//public GenericArrayType(int id, MList<MNode> genArgs, Semantics sem, NodeIterator it) : base(id, null)
		public GenericArrayType(CodeTypes types, int id, DataTypeDef _itemType, int _itemCount, int _accessorID) : base(id, null)
		{
			itemType = _itemType;
			itemCount = _itemCount;
			accessorID = _accessorID;

			// create a callback that generator finds with certain arguments "thisKindOfArray @getAt index"
			types.CreateCallback(
				_accessorID,
				ArgType.Data(itemType),		// return value
				MC.basics.IntType.SizeOf(),	// index type
				Accessor					// callback to call when executing the code
			);
		}

		private void Accessor(MeanMachine mm, MArgs args)
		{
			MS.Verbose("GenericArrayType.Getter");
			// read args from stack and push item address
			int arrayAddress = mm.stack[mm.stackTop - 2];
			int index = mm.stack[mm.stackTop - 1];
			MS.Assertion(index >= 0 && index < itemCount, MC.EC_CODE, "index out of bounds: " + index + "/" + itemCount);
			// NOTE: heap ID on edelleen siellä mutta ei pitäisi muutta koska se on ylemmissä biteissä (0xffff0000)
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
			return args.Size() == 1 && args.First().Def.ID == MC.BASIC_TYPE_INT;
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
		public class Factory : GenericFactory
		{
			public override int CodeID()
			{
				return MC.BASIC_TYPE_GENERIC_CHARS;
			}

			public override GenericType Create(int id, MList<MNode> genArgs, CodeTypes types, NodeIterator it)
			{
				MS.SyntaxAssertion(genArgs.Size() == 1, it, "chars: 1 arguments expected");
				int maxChars = MS.ParseInt(genArgs.Last().data.GetString());
				MS.Assertion(maxChars > 0 && maxChars < 64000, MC.EC_SCRIPT, "chars: unacceptable count: " + maxChars);
			
				int size = (maxChars / 4) + 2;

				// eg. "moi!" ->
				// 0: [4 = numChars]
				// 1: ['m''o''i''!']
				// 2: [    '\0'    ]

				return new GenericCharsType(id, maxChars, size);
			}
			
			public override void Encode(ByteCode bc, GenericType t)
			{
				var ct = (GenericCharsType)t;
				bc.AddInstruction(MC.OP_GENERIC_TYPE, 3, CodeID());
				bc.AddWord(ct.ID);
				bc.AddWord(ct.maxChars);
				bc.AddWord(ct.size);
			}
			public override GenericType Decode(MeanMachine mm, int[] args)
			{
				return new GenericCharsType(
					args[0],	// type ID
					args[1],	// maxChars
					args[2]);	// size
			}

			public override int NumArgs()
			{
				return 3;
			}

			public override string TypeName()
			{
				return "chars";
			}
		}
		public readonly int maxChars;
		public readonly int size;

		public GenericCharsType(int id, int _maxChars, int _size) : base(id, null)
		{
			maxChars = _maxChars;
			size = _size;
		}

		public override bool TypeMatch(MList<ArgType> args)
		{
			return args.Size() == 1 && args.First().Def.ID == MC.BASIC_TYPE_TEXT;
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
