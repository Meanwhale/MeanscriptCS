namespace Meanscript
{
	using Core;

	public abstract class IMSData
	{
		// code common for MSData and MSStruct
		// TODO: accessor for arrays, etc.

		protected readonly MeanMachine mm;
		protected readonly int typeID,
			heapID,
			offset;  // address of the data
		//readonly IntArray structCode;    // where struct info is
		protected readonly IntArray dataCode;  // where actual data is

		protected IMSData(MeanMachine mm, int typeID, int heapID, int offset)
		{
			this.mm = mm;
			this.typeID = typeID;
			this.heapID = heapID;
			this.offset = offset;
			dataCode = mm.Heap.GetDataArray(heapID);
		}
	}

	public class MSData : IMSData
	{
		// accessor to data of any type

		public MSData(MeanMachine mm, int typeID, int heapID, int offset) : base (mm, typeID, heapID, offset)
		{
		}
		public int GetInt()
		{
			MS.Assertion(typeID == MC.BASIC_TYPE_INT, MC.EC_DATA, "not an int");
			return dataCode[offset];
		}

		internal MSStruct GetStruct()
		{
			TypeDef memberType = mm.codeTypes.GetType(typeID);
			MS.Assertion(memberType is StructDefType, MC.EC_DATA, "data not a struct");
			return new MSStruct(mm, memberType.ID, heapID, offset);
		}

		internal bool Match(MSData x)
		{
			if (typeID != x.typeID) return false;
			TypeDef memberType = mm.codeTypes.GetType(typeID);
			// compare 'raw' data in data arrays
			return IntArray.Match(dataCode, offset, x.dataCode, x.offset, memberType.SizeOf());
		}
		// TODO: add getters for all basic types
	}

	public class MSStruct : IMSData
	{
		public readonly StructDefType structType;

		public MSStruct(MeanMachine mm, int typeID, int heapID, int offset) : base (mm, typeID, heapID, offset)
		{
			var type = mm.codeTypes.GetType(typeID);
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
		//public MSStruct GetStruct(string typename, string name)
		//{
		//	var sd = mm.types.GetType(new MSText(typename));
		//	MS.Assertion(sd != null, MC.EC_DATA, "struct not found: " + name);
		//	var member = GetMember(name, sd.ID);
		//	MS.Assertion (member.Type.ID == sd.ID);
		//	return new MSStruct(mm, sd.ID, heapID, member.Address + offset);
		//}
		internal MSStruct GetStruct(string name)
		{
			var member = structType.SD.GetMember(new MSText(name));
			MS.Assertion(member != null, MC.EC_DATA, "member not found: " + name);
			TypeDef memberType = mm.codeTypes.GetType(member.Type.ID);
			MS.Assertion(memberType is StructDefType, MC.EC_DATA, "member is not a struct: " + name);
			return new MSStruct(mm, memberType.ID, heapID, member.Address + offset);
		}
		
		internal MSData GetData(string name)
		{
			var member = structType.SD.GetMember(new MSText(name));
			MS.Assertion(member != null, MC.EC_DATA, "member not found: " + name);
			return GetData(member);
		}
		internal MSData GetData(StructDef.Member member)
		{
			MS.Assertion(!(member.Type is StructDefType), MC.EC_DATA, "member is a struct: " + mm.GetText(member.NameID));
			return new MSData(mm, member.Type.ID, heapID, member.Address + offset);
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
			int heapID = MHeap.TagIndex(tag);
			int typeID = MHeap.TagType(tag);
			MS.Assertion(mm.Heap.HasObject(heapID), MC.EC_DATA, "invalid reference: " + tag + ", heap ID " + heapID);
			return new MSData(mm, typeID, heapID, 0);
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
		/*
		public bool IsStruct()
		{
			return typeID == 0 || typeID >= MAX_MS_TYPES;
		}

		public bool HasData(string name)
		{
			int memberTagAddress = GetMemberTagAddress(name, false);
			return memberTagAddress >= 0;
		}

		public bool HasArray(string name)
		{
			int memberTagAddress = GetMemberTagAddress(name, true);
			return memberTagAddress >= 0;
		}

		public string GetText()
		{
			MS.Assertion(DataType() >= MS_TYPE_TEXT, MC.EC_INTERNAL, "not a text");
			return GetText(dataCode[dataIndex]);
		}

		public string GetText(string name)
		{
			int address = GetMemberAddress(name, MS_TYPE_TEXT);
			MS.Assertion(address >= 0, EC_DATA, "unknown name");
			return GetText(dataCode[address]);
		}
		public string GetText(int id)
		{
			return mm.GetText(id);
		}
		public MSText GetMSText(int id)
		{
			if (id == 0) return new MSText("");
			int address = mm.texts[id]; // operation address
			return new MSText(structCode, address + 1);
		}

		public bool IsChars(int typeID)
		{
			return false;
			//int charsTypeAddress = mm.types[typeID];
			//int charsTypeTag = structCode[charsTypeAddress];
			//return IsCharsTag(charsTypeTag);
		}

		public int GetCharsSize(int typeID)
		{
			//int index = mm.types[typeID];
			//uint typeTagID = (uint)(structCode[index] & VALUE_TYPE_MASK);
			//int potentialCharsTag = structCode[mm.types[typeTagID]];
			//if (IsCharsTag(potentialCharsTag))
			//{
			//	return structCode[mm.types[typeTagID] + 1];
			//}
			//else
			return -1;
		}

		public string GetChars(string name)
		{
			int memberTagAddress = GetMemberTagAddress(name, false);

			int charsTag = structCode[memberTagAddress];
			MS.Assertion(IsCharsTag(charsTag), EC_DATA, "not chars");

			MS.Assertion(memberTagAddress >= 0, EC_DATA, "not found: " + name);
			int address = dataIndex + structCode[memberTagAddress + 2];
			int numChars = dataCode[address];
			string s = System.Text.Encoding.UTF8.GetString(MS.IntsToBytes(dataCode, address + 1, numChars));
			return s;
		}

		public float GetFloat()
		{
			MS.Assertion(DataType() != MS_TYPE_FLOAT, MC.EC_INTERNAL, "not a float");
			return MS.IntFormatToFloat(dataCode[dataIndex]);
		}


		public int GetInt()
		{
			MS.Assertion(DataType() >= MS_TYPE_INT, MC.EC_INTERNAL, "not a 32-bit integer");
			return dataCode[dataIndex];
		}
		*/

		
		public int GetInt(string name)
		{
			var member = GetMember(name, MC.BASIC_TYPE_INT);
			return dataCode[member.Address + offset];
		}
		public bool GetBool(string name)
		{
			int address = GetMemberAddress(name, MC.BASIC_TYPE_BOOL);
			MS.Assertion(address >= 0, MC.EC_DATA, "unknown name");
			return dataCode[address] != 0;
		}
		public long GetInt64(string name)
		{
			int address = GetMemberAddress(name, MC.BASIC_TYPE_INT64);
			return GetInt64At(address);
		}
		public long GetInt64At(int address)
		{
			int a = dataCode[address];
			int b = dataCode[address + 1];
			long i64 = MC.IntsToInt64(a, b);
			return i64;
		}
		public float GetFloat(string name)
		{
			int address = GetMemberAddress(name, MC.BASIC_TYPE_FLOAT);
			MS.Assertion(address >= 0, MC.EC_DATA, "unknown name");
			return MS.IntFormatToFloat(dataCode[address]);
		}
		public double GetFloat64(string name)
		{
			int address = GetMemberAddress(name, MC.BASIC_TYPE_FLOAT64);
			return GetFloat64At(address);
		}
		public double GetFloat64At(int address)
		{
			long i64 = GetInt64At(address);
			return MS.Int64FormatToFloat64(i64);
		}
		public string GetChars(string name)
		{
			var member = GetMember(name);
			MS.Assertion(member.Type is GenericCharsType, MC.EC_DATA, "not a chars[n]: " + name);
			int address = offset + member.Address;
			int numChars = dataCode[address];
			return System.Text.Encoding.UTF8.GetString(MS.IntsToBytes(dataCode, address + 1, numChars));
		}
		public string GetText(string name)
		{
			var member = GetMember(name);
			MS.Assertion(member.Type.ID == MC.BASIC_TYPE_TEXT, MC.EC_DATA, "not a text " + name);
			int address = offset + member.Address;
			int textID = dataCode[address];
			return mm.GetText(textID);
		}
		/*
public MSDataArray GetArray(string name)
{
	int arrayTagAddress = GetMemberTagAddress(name, true);
	MS.Assertion(arrayTagAddress >= 0, EC_DATA, "not found: " + name);
	int arrayTag = structCode[arrayTagAddress];
	MS.Assertion(IsArrayTag(arrayTag), EC_DATA, "not an array");
	int dataAddress = dataIndex + structCode[arrayTagAddress + 2];
	int itemType = structCode[arrayTagAddress + 4];
	int itemCount = structCode[arrayTagAddress + 5];
	int itemDataSize = structCode[arrayTagAddress + 3] / itemCount; // == "sizeof" / itemCount
	return new MSDataArray(mm, itemType, itemCount, itemDataSize, dataAddress);
}

public MSData GetMember(string name)
{
	int memberTagAddress = GetMemberTagAddress(name, false);
	MS.Assertion(memberTagAddress >= 0, EC_DATA, "not found: " + name);
	int dataAddress = dataIndex + structCode[memberTagAddress + 2];
	int _typeID = InstrValueTypeID(structCode[memberTagAddress]);
	return new MSData(mm, _typeID, dataAddress);
}

public int GetMemberAddress(string name, int type)
{
	int memberTagAddress = GetMemberTagAddress(name, false);
	MS.Assertion(memberTagAddress >= 0, EC_DATA, "not found: " + name);
	MS.Assertion(((structCode[memberTagAddress]) & VALUE_TYPE_MASK) == type, EC_DATA, "wrong type");
	return dataIndex + structCode[memberTagAddress + 2];
}

public int GetMemberAddress(string name)
{
	int memberTagAddress = GetMemberTagAddress(name, false);
	MS.Assertion(memberTagAddress >= 0, EC_DATA, "not found: " + name);
	// address of this data + offset of the member
	return dataIndex + structCode[memberTagAddress + 2];
}

public int GetMemberTagAddress(string name, bool isArray)
{
	MSText t = new MSText(name);
	return GetMemberTagAddress(t, isArray);
}

public int GetMemberTagAddress(MSText name, bool isArray)
{
	int nameID = mm.GetTextID(name);
	if (nameID < 0) return -1;
	return GetMemberTagAddress(nameID, isArray);
}

public int GetMemberTagAddress(int nameID, bool isArray)
{
	//MS.Assertion(IsStruct(), MC.EC_INTERNAL, "struct expected");

	//int i = mm.types[DataType()];
	//int code = structCode[i];
	//MS.Assertion((code & OPERATION_MASK) == OP_STRUCT_DEF, MC.EC_INTERNAL, "struct def. expected");
	//i += InstrSize(code) + 1;
	//code = structCode[i];


	//while ((code & OPERATION_MASK) == OP_STRUCT_MEMBER || (code & OPERATION_MASK) == OP_GENERIC_MEMBER)
	//{
	//	if (nameID == structCode[i + 1])
	//		return i; // name ID is immediately after the operator

	//	i += InstrSize(code) + 1;
	//	code = structCode[i];
	//}
	return -1;
}

public void PrintType(MSOutputPrint op, int typeID)
{
	//if (typeID < MAX_MS_TYPES) op.Print(primitiveNames[typeID]);
	//else
	//{
	//	int charsSizeOrNegative = GetCharsSize(typeID);
	//	if (charsSizeOrNegative >= 0)
	//	{
	//		op.Print("chars[");
	//		op.Print(charsSizeOrNegative);
	//		op.Print("]");
	//	}
	//	else
	//	{
	//		int index = mm.types[typeID];
	//		MSText typeName = new MSText(structCode, index + 1);
	//		op.Print(typeName);
	//	}
	//}
}
*/
		//public void PrintData(MSOutputPrint op, int depth, string name)
		//{
		//	//for (int x=0; x<depth; x++) op.print("    ");

		//	if (!IsStruct())
		//	{
		//		if (depth != 1) op.Print(name);
		//		op.Print(": ");
		//		if (DataType() == MS_TYPE_INT)
		//		{
		//			op.Print(dataCode[dataIndex]);
		//		}
		//		else if (DataType() == MS_TYPE_INT64)
		//		{
		//			op.Print(IntsToInt64(dataCode[dataIndex], dataCode[dataIndex + 1]));
		//		}
		//		else if (DataType() == MS_TYPE_FLOAT)
		//		{
		//			op.Print(MS.IntFormatToFloat(dataCode[dataIndex]));
		//		}
		//		else if (DataType() == MS_TYPE_FLOAT64)
		//		{
		//			op.Print(GetFloat64At(dataIndex));
		//		}
		//		else if (DataType() == MS_TYPE_TEXT)
		//		{
		//			MSText tmp = GetMSText(dataCode[dataIndex]);
		//			op.Print("\"");
		//			op.PrintIntsToChars(tmp.GetData(), 1, tmp.NumBytes(), true);
		//			op.Print("\"");
		//		}
		//		else if (DataType() == MS_TYPE_BOOL)
		//		{
		//			if (dataCode[dataIndex] == 0) op.Print("false");
		//			else op.Print("true");
		//		}
		//		else
		//		{
		//			MS.Assertion(false, EC_DATA, "printData: unhandled data type: " + DataType());
		//		}
		//		op.Print("\n");
		//	}
		//	else
		//	{
		//		// NOTE: similar to getMemberTagAddress()

		//		// TODO after generics refactoring is done

		//		op.Print("TODO\n");

		//		/*F

		//		INT i = R(mm).types[getType()];

		//		INT code = R(structCode)[i];



		//		if ((code & OPERATION_MASK) == OP_CHARS_DEF)

		//		{

		//			INT numChars = R(dataCode)[dataIndex + 0];

		//			if (depth != 1) op.print(name);

		//			op.print(": \"");

		//			op.printIntsToChars(R(dataCode), dataIndex + 1, numChars, true);

		//			op.print("\"\n");

		//			return;

		//		}

		//		if (depth == 1) op.print("\n");



		//		ASSERT((code & OPERATION_MASK) == OP_STRUCT_DEF, "printData: struct def. expected");



		//		i += instrSize(code) + 1;

		//		code = R(structCode)[i];

		//		while ((code & OPERATION_MASK) == OP_MEMBER_NAME)

		//		{

		//			STRING s = INTS_TO_STRING(R(structCode), i + 2, R(structCode)[i+1]);



		//			i += instrSize(code) + 1;

		//			code = R(structCode)[i];



		//			// print type name



		//			if (depth == 0) 

		//			{

		//				INT typeID = (INT)(code & VALUE_TYPE_MASK);

		//				printType(op, typeID);

		//				op.print(" ");

		//				op.print(s);

		//			}



		//			if ((code & OPERATION_MASK) == OP_STRUCT_MEMBER)

		//			{

		//				INT dataAddress = dataIndex + R(structCode)[i + 2];

		//				MSData d = NEW_COPY(MSData) (mm, i, dataAddress, false);

		//				if (depth > 0) d.printData(op, depth + 1, name + "." + s);

		//				else d.printData(op, depth + 1, s);

		//			}

		//			else if ((code & OPERATION_MASK) == OP_ARRAY_MEMBER)

		//			{

		//				INT dataAddress = dataIndex + R(structCode)[i + 1];

		//				MSDataArray a = NEW_COPY(MSDataArray) (mm, i, dataAddress);



		//				//for (INT x=0; x<depth+1; x++) op.print("    ");

		//				//op.print(s);





		//				// iterate thru array items



		//				for (INT n=0; n<a.getArrayItemCount(); n++)

		//				{

		//					MSData d = a.getAt(n);

		//					//for (INT x=0; x<depth; x++) op.print("  ");





		//					STRING tmp = "";

		//					if (depth > 0) tmp = name + "." + s;

		//					else tmp = s;	



		//					INodeType.STRING(indexText, n);

		//					tmp += "[";

		//					tmp += indexText;

		//					tmp += "]";

		//					d.printData(op, depth + 1, tmp);

		//				}

		//			}

		//			else

		//			{

		//				ASSERT(false, "broken struct code");

		//			}



		//			i += instrSize(code) + 1;

		//			code = R(structCode)[i];

		//		}

		//		*/
		//	}
		//}


		//;

	}
}
