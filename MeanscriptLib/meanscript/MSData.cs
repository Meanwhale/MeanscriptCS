namespace Meanscript
{
	using Core;
	using System;
	using System.Collections.Generic;

	public abstract class IMSData
	{
		// code common for MSData and MSStruct
		// TODO: accessor for arrays, etc.

		protected readonly CodeTypes types;
		protected MCHeap heap;
		public readonly int typeID,
			heapID,
			offset;  // address of the data
		public readonly IntArray dataCode;  // where actual data is
		
		protected IMSData(CodeTypes types, MCHeap heap, int typeID, int heapID, int offset)
		{
			this.types = types;
			this.heap = heap;
			this.typeID = typeID;
			this.heapID = heapID;
			this.offset = offset;
			dataCode = heap.GetStoreData(heapID);
		}
		protected IMSData(CodeTypes types, int typeID, IntArray dataCode, int offset, MCHeap heap)
		{
			this.heap = heap;
			heapID = -1;

			this.types = types;
			this.typeID = typeID;
			this.offset = offset;
			this.dataCode = dataCode;
		}
		protected IMSData(CodeTypes types, int typeID)
		{
			// data object that owns its storage (dataCode)
			heap = null;
			heapID = -1;
			offset = 0;

			this.types = types;
			this.typeID = typeID;
			dataCode = new IntArray(types.GetDataType(typeID).SizeOf());
		}
		public bool IsBuilderData()
		{
			return heap == null && heapID == -1 && offset == 0;
		}
		public MSData this[string key]
		{
			get => GetValue(key);
			set => SetValue(key, value);
		}

		public virtual void SetValue(string key, IMSData value)
		{
			MS.Assertion(false, MC.EC_DATA, "set [string] is not defined for this type: " + TypeName());
		}

		public string TypeName()
		{
			return types.GetTypeDef(typeID).TypeNameString();
		}

		public virtual MSData GetValue(string key)
		{
			MS.Assertion(false, MC.EC_DATA, "get [string] is not defined for this type: " + TypeName());
			return null;
		}
		public MSData this[int key]
		{
			get => GetValue(key);
			set => SetValue(key, value);
		}
		public virtual void SetValue(int key, IMSData value)
		{
			MS.Assertion(false, MC.EC_DATA, "set [int] is not defined for this type: " + TypeName());
		}
		public virtual MSData GetValue(int key)
		{
			MS.Assertion(false, MC.EC_DATA, "get [int] is not defined for this type: " + TypeName());
			return null;
		}
	}

	public class MSData : IMSData
	{
		// accessor to data of any type
		
		public MSData(CodeTypes codeTypes, int typeID) : base(codeTypes, typeID)
		{			
		}
		public MSData(CodeTypes types, int typeID, IntArray dataCode, int offset, MCHeap heap) : base(types, typeID, dataCode, offset, heap)
		{
		}
		public MSData(MeanMachine mm, int typeID, int heapID, int offset) : base (mm.codeTypes, mm.Heap, typeID, heapID, offset)
		{
		}
		public MSData(CodeTypes types, MCHeap heap, int typeID, int heapID, int offset) : base (types, heap, typeID, heapID, offset)
		{
		}
		public int Int()
		{
			MS.Assertion(typeID == MC.BASIC_TYPE_INT, MC.EC_DATA, "not an int");
			return dataCode[offset];
		}
		public void SetInt(int a)
		{
			MS.Assertion(typeID == MC.BASIC_TYPE_INT, MC.EC_DATA, "not an int");
			dataCode[offset] = a;
		}
		public bool Bool()
		{
			MS.Assertion(typeID == MC.BASIC_TYPE_BOOL, MC.EC_DATA, "not a bool");
			return dataCode[offset] != 0;
		}
		public long Int64()
		{
			MS.Assertion(typeID == MC.BASIC_TYPE_INT64, MC.EC_DATA, "not an int64");
			return GetInt64At(offset);
		}
		public long GetInt64At(int address)
		{
			int a = dataCode[address];
			int b = dataCode[address + 1];
			long i64 = MC.IntsToInt64(a, b);
			return i64;
		}
		public float Float()
		{
			MS.Assertion(typeID == MC.BASIC_TYPE_FLOAT, MC.EC_DATA, "not a float");
			return MS.IntFormatToFloat(dataCode[offset]);
		}
		public double Float64()
		{
			long i64 = GetInt64At(offset);
			return MS.Int64FormatToFloat64(i64);
		}
		public string Chars()
		{
			var type = types.GetDataType(typeID);
			MS.Assertion(type is GenericCharsType, MC.EC_DATA, "not a chars[n]");
			int numChars = dataCode[offset];
			return System.Text.Encoding.UTF8.GetString(MS.GetIntsToBytesLE(dataCode, offset + 1, numChars));
		}
		public string Text()
		{
			MS.Assertion(typeID == MC.BASIC_TYPE_TEXT, MC.EC_DATA, "not a text");
			int textID = dataCode[offset];
			return types.texts.FindTextStringByID(textID);
		}

		internal MSStruct Struct()
		{
			TypeDef memberType = types.GetTypeDef(typeID);
			MS.Assertion(memberType is StructDefType, MC.EC_DATA, "data not a struct");
			return new MSStruct(types, heap, memberType.ID, heapID, offset);
		}

		internal bool Match(MSData x)
		{
			if (typeID != x.typeID) return false;
			TypeDef memberType = types.GetTypeDef(typeID);
			// compare 'raw' data in data arrays
			return IntArray.Match(dataCode, offset, x.dataCode, x.offset, memberType.SizeOf());
		}

		internal MSArray GetArray()
		{
			return new MSArray(types, typeID, dataCode, offset, heap);
		}

		internal MSMap GetMap()
		{
			// NOTE: MSData might not have a heap, so it could be given to it
			var type = types.GetTypeDef(typeID);
			MS.Assertion(type != null, MC.EC_DATA, "type not found by id " + typeID);
			MS.Assertion(type.ID == MC.BASIC_TYPE_MAP, MC.EC_DATA, "not a map");
			MS.Assertion(heap != null);

			int tag = dataCode[offset];
			int heapID = MCHeap.TagIndex(tag);
			return heap.GetMapAt(heapID).map;
		}

		// TODO: add getters for all basic types

		// operator []

		public override MSData GetValue(string key)
		{
			// check if data is some type that supports []

			TypeDef t = types.GetTypeDef(typeID);
			if (t.ID == MC.BASIC_TYPE_MAP)
			{
				return GetMap().Get(key);
			}
			else if (t is StructDefType)
			{
				return Struct().GetValue(key);
			}
			return base.GetValue(key);
		}
		public override MSData GetValue(int key)
		{
			TypeDef t = types.GetTypeDef(typeID);
			if (t is GenericArrayType)
			{
				return GetArray().GetValue(key);
			}
			return base.GetValue(key);
		}
	}

	public class MSMap
	{
		private CodeTypes types;
		private MCHeap heap;
		public Dictionary<string,IntArray> dict = new Dictionary<string, IntArray>();
		internal int tag = 0;

		public MSMap(CodeTypes types, MCHeap heap, int tag)
		{
			this.types = types;
			this.heap = heap;
			this.tag = tag;
		}
		public MSData Get(string key)
		{
			// value data: OP_MAP_VALUE + data
			var data = dict[key];
			int op = data[0];
			MS.Assertion((op & MC.OPERATION_MASK) == MC.OP_SET_MAP_VALUE);
			var typeID = MC.InstrValueTypeID(op);
			return new MSData(types, typeID, data, 1, heap); // NOTE: direct access to map data
		}
		public void Set(string key, MSData val)
		{
			MS.Assertion(!dict.ContainsKey(key));
			var t = types.GetTypeDef(val.typeID);
			int valueSize = t.SizeOf();

			// value data: OP_SET_MAP_VALUE + data
			var data = new IntArray(valueSize + 1);
			data[0] = MC.MakeInstruction(MC.OP_SET_MAP_VALUE, valueSize, val.typeID);
			IntArray.Copy(val.dataCode, 0, data, 1, valueSize);
			dict[key] = data;
		}
		private void Set(string key, IMSData value)
		{
			if (value is MSData x)
			{
				Set(key, x);
			}
			else MS.Assertion(false , MC.EC_DATA, "MSData type expected");
		}
		public IMSData this[string key]
		{
			get => Get(key);
			set => Set(key, value);
		}
	}

	public class MSArray : IMSData
	{
		private GenericArrayType arrayType;

		public DataTypeDef ItemType { get { return arrayType.itemType; } }
		public int Length { get { return arrayType.itemCount; } }

		internal MSArray(CodeTypes types, int typeID, IntArray dataCode, int offset, MCHeap heap) : base(types, typeID, dataCode, offset, heap)
		{
			Init();
		}
		private void Init()
		{
			var type = types.GetTypeDef(typeID);
			MS.Assertion(type != null, MC.EC_DATA, "type not found, id: " + typeID);
			MS.Assertion(type is GenericArrayType, MC.EC_DATA, "not an array, id: " + typeID);
			arrayType = (GenericArrayType)type;
		}
		public MSData At(int index)
		{
			MS.Assertion(index >= 0 && index < Length, MC.EC_DATA, "index out of bounds: " + index + " / " + Length);
			return new MSData(types, ItemType.ID, dataCode, offset + (index * ItemType.SizeOf()), heap);
		}
		public override MSData GetValue(int index)
		{
			return At(index);
		}
	}

	public class MSStruct : IMSData
	{
		public StructDefType structType;
		
		public MSStruct(CodeTypes codeTypes, int typeID) : base(codeTypes, typeID)
		{	
			Init();		
		}
		public MSStruct(CodeTypes types, int typeID, IntArray dataCode, int offset, MCHeap heap) : base(types, typeID, dataCode, offset, heap)
		{
			Init();
		}
		public MSStruct(MeanMachine mm, int typeID, int heapID, int offset) : base (mm.codeTypes, mm.Heap, typeID, heapID, offset)
		{
			Init();
		}
		public MSStruct(CodeTypes types, MCHeap heap, int typeID, int heapID, int offset) : base (types, heap, typeID, heapID, offset)
		{
			Init();
		}

		private void Init()
		{
			var type = types.GetTypeDef(typeID);
			MS.Assertion(type != null, MC.EC_DATA, "type not found, id: " + typeID);
			MS.Assertion(type is StructDefType, MC.EC_DATA, "not a struct, id: " + typeID);
			structType = (StructDefType)type;
		}

		public int DataType()
		{
			return typeID;
		}
		public int NumMembers()
		{
			return structType.SD.NumMembers();
		}
		public StructDef.Member GetMemberAt(int index)
		{
			return structType.SD.GetMemberByIndex(index);
		}
		public StructDef.Member GetMember(string name, int typeID = -1)
		{
			var member = structType.SD.GetMember(new MSText(name));
			MS.Assertion(member != null, MC.EC_DATA, "member not found: " + name);
			MS.Assertion(typeID < 0 || member.Type.ID == typeID, MC.EC_DATA, "type mismatch");
			return member;
		}
		public int GetMemberAddress(string name, int typeID)
		{
			var member = structType.SD.GetMember(new MSText(name));
			MS.Assertion(member != null, MC.EC_DATA, "member not found: " + name);
			MS.Assertion(member.Type.ID == typeID, MC.EC_DATA, "type mismatch");
			return member.Address + offset;
		}
		internal MSStruct GetStruct(string name)
		{
			var member = structType.SD.GetMember(new MSText(name));
			MS.Assertion(member != null, MC.EC_DATA, "member not found: " + name);
			TypeDef memberType = types.GetTypeDef(member.Type.ID);
			MS.Assertion(memberType is StructDefType, MC.EC_DATA, "member is not a struct: " + name);
			return new MSStruct(types, heap, memberType.ID, heapID, member.Address + offset);
		}
		internal MSArray GetArray(string name)
		{
			return GetData(name).GetArray();
		}
		internal MSData GetData(string name)
		{
			var member = structType.SD.GetMember(new MSText(name));
			MS.Assertion(member != null, MC.EC_DATA, "member not found: " + name);
			return GetData(member);
		}
		internal MSData GetData(string name, int typeID)
		{
			var member = structType.SD.GetMember(new MSText(name));
			MS.Assertion(member != null, MC.EC_DATA, "member not found: " + name);
			MS.Assertion(member.Type.ID == typeID, MC.EC_DATA, () => {
				var td = types.GetTypeDef(typeID);
				string s = (td == null ? typeID.ToString() : td.TypeNameString());
				return name + " is not of type " + td;
			});
			return GetData(member);
		}
		internal MSData GetData(StructDef.Member member)
		{
			//return new MSData(types, member.Type.ID, dataCode, offset + member.Address, heap);
			return new MSData(types, heap, member.Type.ID, heapID, offset + member.Address);
		}
		internal MSData GetRef(string name)
		{
			// get data of ObjectType (obj[x])
			// read the tag address (defined in MHeap) of the variable
			// and get data object

			var member = structType.SD.GetMember(new MSText(name));
			MS.Assertion(member != null, MC.EC_DATA, "member not found: " + name);
			MS.Assertion(member.Type is ObjectType, MC.EC_DATA, "not an object reference: " + name);
			int tag = dataCode[member.Address + offset];
			int heapID = MCHeap.TagIndex(tag);
			int typeID = MCHeap.TagType(tag);
			MS.Assertion(heap.HasObject(heapID), MC.EC_DATA, "invalid reference: " + tag + ", heap ID " + heapID);
			return new MSData(types, heap, typeID, heapID, 0);
		}
		internal bool Match(MSStruct x)
		{
			if (NumMembers() != x.NumMembers()) return false;
			for(int i = 0; i < NumMembers(); i++)
			{
				// TODO: tämä testaa hyvin structin toimivuutta, mutta
				// nopeampaa on verrata vain suoraan dataa niinkuin MSDatassa.
				var a = GetMemberAt(i);
				var b = x.GetMemberAt(i);
				if (a.Type.ID != b.Type.ID) return false;
				// TODO: check if struct (virtual Match()?)
				var aData = GetData(a);
				var bData = GetData(b);
				if (!aData.Match(bData)) return false;
			}
			return true;
		}

		internal MSData GetData(MSBuilderMember bm)
		{
			MS.Assertion(IsBuilderData());
			return new MSData(types, bm.MemberType.ID, dataCode, offset + bm.SDMember.Address, heap);
		}

		internal MSData GetObj(string name)
		{
			var data = GetData(name);
			var type = types.GetTypeDef(data.typeID);
			if (type is ObjectType)
			{
				// get object tag and the heap object it points to
				int tag = dataCode[data.offset + offset];
				var ddata = heap.GetStoreByIndex(MCHeap.TagIndex(tag));
				return new MSData(types, heap, ddata.DataTypeID(), MCHeap.TagIndex(tag), 0);
			}
			MS.Assertion(false, MC.EC_DATA, "not an object type: " + name);
			return null;
		}

		public int GetInt(string name)
		{
			return GetData(name, MC.BASIC_TYPE_INT).Int();
		}
		public bool GetBool(string name)
		{
			return GetData(name, MC.BASIC_TYPE_BOOL).Bool();
		}
		public long GetInt64(string name)
		{
			return GetData(name, MC.BASIC_TYPE_INT64).Int64();
		}
		public float GetFloat(string name)
		{
			return GetData(name, MC.BASIC_TYPE_FLOAT).Float();
		}
		public double GetFloat64(string name)
		{
			return GetData(name, MC.BASIC_TYPE_FLOAT64).Float64();
		}
		public string GetChars(string name)
		{
			var member = GetMember(name);
			MS.Assertion(member.Type is GenericCharsType, MC.EC_DATA, "not a chars[n]: " + name);
			return GetData(name).Chars();
		}
		public string GetText(string name)
		{
			var member = GetMember(name);
			MS.Assertion(member.Type.ID == MC.BASIC_TYPE_TEXT, MC.EC_DATA, "not a text " + name);
			return GetData(name).Text();
		}
		
		// operator []

		public override MSData GetValue(string key)
		{
			return GetData(key);
		}
	}
}
