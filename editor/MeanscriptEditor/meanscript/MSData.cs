namespace Meanscript {

public class MSData : MC {
MeanMachine mm;
int typeID,
	dataIndex;  // address of the data
IntArray structCode;	// where struct info is
IntArray dataCode;	// where actual data is

// TODO: make a map if wanted
// MAP_STRING_TO_INT(globalNames);

public MSData (MeanMachine _mm, int _typeID, int _dataIndex) 
{
	MS.assertion(_typeID >= 0 && _typeID < MAX_TYPES,MC.EC_INTERNAL, "invalid type ID: " + _typeID);
	
	mm = _mm;
	
	structCode = mm.getStructCode();
	dataCode = mm.getDataCode();

	mm = _mm;
	typeID = _typeID;
	dataIndex = _dataIndex;
}

public int getType ()
{
	return typeID;
}

public bool isStruct ()
{
	return typeID == 0 || typeID >= MAX_MS_TYPES;
}

public bool hasData (string name) 
{
	int memberTagAddress = getMemberTagAddress(name, false);
	return memberTagAddress >= 0;
}

public bool hasArray (string name) 
{
	int memberTagAddress = getMemberTagAddress(name, true);
	return memberTagAddress >= 0;
}

public string getText () 
{
	MS.assertion(getType() >= MS_TYPE_TEXT,MC.EC_INTERNAL, "not a text");
	return getText(dataCode[dataIndex]);
}

public string getText (string name) 
{
	int address = getMemberAddress(name, MS_TYPE_TEXT);
	MS.assertion(address >= 0, EC_DATA, "unknown name");
	return getText(dataCode[address]);
}
public string  getText (int id)
{
	if (id == 0) return "";
	int address = mm.texts[id];
	int numChars = structCode[address + 1];
	string s = System.Text.Encoding.UTF8.GetString(MS.intsToBytes(structCode, address + 2, numChars));
	return s;
}
public MSText  getMSText (int id) 
{
	if (id == 0) return new MSText("");
	int address = mm.texts[id]; // operation address
	return new MSText(structCode,address+1);
}

public bool isChars (int typeID)
{
	int charsTypeAddress = mm.types[typeID];
	int charsTypeTag = structCode[charsTypeAddress];
	return isCharsTag(charsTypeTag);
}

public int getCharsSize (int typeID)
{
	int index = mm.types[typeID];
	uint typeTagID = (uint)(structCode[index] & VALUE_TYPE_MASK);
	int potentialCharsTag = structCode[mm.types[typeTagID]];
	if (isCharsTag(potentialCharsTag))
	{
		return structCode[mm.types[typeTagID] + 1];
	}
	else return -1;
}

public string getChars (string name) 
{
	int memberTagAddress = getMemberTagAddress(name, false);
	
	int charsTag = structCode[memberTagAddress];
	int typeID = instrValueTypeID(charsTag);
	MS.assertion(isCharsTag(charsTag), EC_DATA, "not chars");
	
	MS.assertion(memberTagAddress >= 0, EC_DATA, "not found: " + name);
	int address = dataIndex + structCode[memberTagAddress + 2];
	int numChars = dataCode[address];
	string s = System.Text.Encoding.UTF8.GetString(MS.intsToBytes(dataCode, address + 1, numChars));
	return s;
}

public float getFloat () 
{
	MS.assertion(getType() != MS_TYPE_FLOAT,MC.EC_INTERNAL, "not a float");
	return MS.intFormatToFloat(dataCode[dataIndex]);
}

public float getFloat (string name) 
{
	int address = getMemberAddress(name, MS_TYPE_FLOAT);
	MS.assertion(address >= 0, EC_DATA, "unknown name");
	return MS.intFormatToFloat(dataCode[address]);
}

public int getInt () 
{
	MS.assertion(getType() >= MS_TYPE_INT,MC.EC_INTERNAL, "not a 32-bit integer");
	return dataCode[dataIndex];
}


public int getInt (string name) 
{
	int address = getMemberAddress(name, MS_TYPE_INT);
	MS.assertion(address >= 0, EC_DATA, "unknown name");
	return dataCode[address];
}

public bool getBool () 
{
	MS.assertion(getType() >= MS_TYPE_BOOL,MC.EC_INTERNAL, "not a bool integer");
	return dataCode[dataIndex] != 0;
}

public bool getBool (string name) 
{
	int address = getMemberAddress(name, MS_TYPE_BOOL);
	MS.assertion(address >= 0, EC_DATA, "unknown name");
	return dataCode[address] != 0;
}

public long getInt64 () 
{
	return getInt64At(dataIndex);
}

public long getInt64 (string name) 
{
	int address = getMemberAddress(name, MS_TYPE_INT64);
	return getInt64At(address);
}
public long getInt64At (int address) 
{
	int a = dataCode[address];
	int b = dataCode[address+1];
	long i64 = intsToInt64(a,b);
	return i64;
}

public double getFloat64 () 
{
	return getFloat64At(dataIndex);
}

public double getFloat64 (string name) 
{
	int address = getMemberAddress(name, MS_TYPE_FLOAT64);
	return getFloat64At(address);
}
public double getFloat64At (int address) 
{
	long i64 = getInt64At(address);
	return MS.int64FormatToFloat64(i64);
}

public MSDataArray getArray (string name) 
{
	int arrayTagAddress = getMemberTagAddress(name, true);
	MS.assertion(arrayTagAddress >= 0, EC_DATA, "not found: " + name);
	int arrayTag = structCode[arrayTagAddress];
	MS.assertion(isArrayTag(arrayTag), EC_DATA, "not an array");
	int dataAddress = dataIndex + structCode[arrayTagAddress + 2];
	int itemType = structCode[arrayTagAddress + 4];
	int itemCount = structCode[arrayTagAddress + 5];
	int itemDataSize = structCode[arrayTagAddress + 3] / itemCount; // == "sizeof" / itemCount
	return new MSDataArray (mm, itemType, itemCount, itemDataSize, dataAddress);
}

public MSData getMember (string name) 
{
	int memberTagAddress = getMemberTagAddress(name, false);
	MS.assertion(memberTagAddress >= 0, EC_DATA, "not found: " + name);
	int dataAddress = dataIndex + structCode[memberTagAddress + 2];
	int _typeID = instrValueTypeID(structCode[memberTagAddress]);
	return new MSData (mm, _typeID, dataAddress);
}

public int getMemberAddress (string name, int type) 
{
	int memberTagAddress = getMemberTagAddress(name, false);
	MS.assertion(memberTagAddress >= 0, EC_DATA, "not found: " + name);
	MS.assertion(((structCode[memberTagAddress]) & VALUE_TYPE_MASK) == type, EC_DATA, "wrong type");
	return dataIndex + structCode[memberTagAddress + 2];
}

public int getMemberAddress (string name) 
{
	int memberTagAddress = getMemberTagAddress(name, false);
	MS.assertion(memberTagAddress >= 0, EC_DATA, "not found: " + name);
	// address of this data + offset of the member
	return dataIndex + structCode[memberTagAddress + 2];
}

public int getMemberTagAddress (string name, bool isArray) 
{
	MSText t = new MSText (name);
	return getMemberTagAddress(t, isArray);
}

public int getMemberTagAddress (MSText name, bool isArray) 
{
	int nameID = mm.getTextID(name);
	if (nameID < 0) return -1;
	return getMemberTagAddress(nameID, isArray);
}

public int getMemberTagAddress (int nameID, bool isArray) 
{
	MS.assertion(isStruct(),MC.EC_INTERNAL, "struct expected");
	
	int i = mm.types[getType()];
	int code = structCode[i];
	MS.assertion((code & OPERATION_MASK) == OP_STRUCT_DEF,MC.EC_INTERNAL, "struct def. expected");
	i += instrSize(code) + 1;
	code = structCode[i];
	
	
	while ((code & OPERATION_MASK) == OP_STRUCT_MEMBER || (code & OPERATION_MASK) == OP_GENERIC_MEMBER)
	{
		if (nameID == structCode[i+1])
			return i; // name ID is immediately after the operator
		
		i += instrSize(code) + 1;
		code = structCode[i];
	}
	return -1;
}

public void  printType (MSOutputPrint op, int typeID) 
{
	if (typeID < MAX_MS_TYPES) op.print(primitiveNames[typeID]);
	else
	{
		int charsSizeOrNegative = getCharsSize(typeID);
		if (charsSizeOrNegative >= 0)
		{
			op.print("chars[");
			op.print(charsSizeOrNegative);
			op.print("]");
		}
		else
		{
			int index = mm.types[typeID];
			MSText typeName = new MSText (structCode,index+1);
			op.print(typeName);
		}
	}
}

public void  printData (MSOutputPrint op, int depth, string name) 
{
	//for (int x=0; x<depth; x++) op.print("    ");
	
	if (!isStruct())
	{
		if (depth != 1) op.print(name);
		op.print(": ");
		if (getType() == MS_TYPE_INT)
		{
			op.print(dataCode[dataIndex]);
		}
		else if (getType() == MS_TYPE_INT64)
		{
			op.print(intsToInt64(dataCode[dataIndex], dataCode[dataIndex+1]));
		}
		else if (getType() == MS_TYPE_FLOAT)
		{
			op.print(MS.intFormatToFloat(dataCode[dataIndex]));
		}
		else if (getType() == MS_TYPE_FLOAT64)
		{
			op.print(getFloat64At(dataIndex));
		}
		else if (getType() == MS_TYPE_TEXT)
		{
			MSText tmp = getMSText(dataCode[dataIndex]);
			op.print("\"");
			op.printIntsToChars(tmp.getData(), 1, tmp.numBytes(), true);
			op.print("\"");
		}
		else if (getType() == MS_TYPE_BOOL)
		{
			if (dataCode[dataIndex] == 0) op.print("false");
			else op.print("true");
		}
		else
		{
			MS.assertion(false, EC_DATA, "printData: unhandled data type: " + getType());
		}
		op.print("\n");
	}
	else
	{	
		// NOTE: similar to getMemberTagAddress()
		
		// TODO after generics refactoring is done
		
		op.print("TODO\n");
		
		/*

		INT i = R(mm).types[getType()];

		INT code = R(structCode)[i];

		

		if ((code & OPERATION_MASK) == OP_CHARS_DEF)

		{

			INT numChars = R(dataCode)[dataIndex + 0];

			if (depth != 1) op.print(name);

			op.print(": \"");

			op.printIntsToChars(R(dataCode), dataIndex + 1, numChars, true);

			op.print("\"\n");

			return;

		}

		if (depth == 1) op.print("\n");

					

		ASSERT((code & OPERATION_MASK) == OP_STRUCT_DEF, "printData: struct def. expected");

				

		i += instrSize(code) + 1;

		code = R(structCode)[i];

		while ((code & OPERATION_MASK) == OP_MEMBER_NAME)

		{

			STRING s = INTS_TO_STRING(R(structCode), i + 2, R(structCode)[i+1]);

	

			i += instrSize(code) + 1;

			code = R(structCode)[i];

			

			// print type name

			

			if (depth == 0) 

			{

				INT typeID = (INT)(code & VALUE_TYPE_MASK);

				printType(op, typeID);

				op.print(" ");

				op.print(s);

			}

			

			if ((code & OPERATION_MASK) == OP_STRUCT_MEMBER)

			{

				INT dataAddress = dataIndex + R(structCode)[i + 2];

				MSData d = NEW_COPY(MSData) (mm, i, dataAddress, false);

				if (depth > 0) d.printData(op, depth + 1, name + "." + s);

				else d.printData(op, depth + 1, s);

			}

			else if ((code & OPERATION_MASK) == OP_ARRAY_MEMBER)

			{

				INT dataAddress = dataIndex + R(structCode)[i + 1];

				MSDataArray a = NEW_COPY(MSDataArray) (mm, i, dataAddress);

					

				//for (INT x=0; x<depth+1; x++) op.print("    ");

				//op.print(s);

				

				

				// iterate thru array items



				for (INT n=0; n<a.getArrayItemCount(); n++)

				{

					MSData d = a.getAt(n);

					//for (INT x=0; x<depth; x++) op.print("  ");





					STRING tmp = "";

					if (depth > 0) tmp = name + "." + s;

					else tmp = s;	

					

					INT_STRING(indexText, n);

					tmp += "[";

					tmp += indexText;

					tmp += "]";

					d.printData(op, depth + 1, tmp);

				}

			}

			else

			{

				ASSERT(false, "broken struct code");

			}



			i += instrSize(code) + 1;

			code = R(structCode)[i];

		}

		*/
	}
}

//;

}
}
