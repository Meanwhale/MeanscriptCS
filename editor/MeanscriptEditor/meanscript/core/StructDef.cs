namespace Meanscript {

public class StructDef : MC {
Semantics semantics;
public int typeID;
internal IntArray code;
internal IntArray tagAddress; // tagAddress[n] = code (above) address of _n_th member
internal int nameID; // reference to Semantics' text
internal int numMembers;
internal int argsSize;
public int structSize;
internal int codeTop;
//internal System.Collections.Generic.Dictionary<MSText, int> memberNames = new System.Collections.Generic.Dictionary<MSText, int>(MS.textComparer);

public StructDef (Semantics _semantics, int _nameID, int _typeID) 
{
	semantics = _semantics;
	nameID = _nameID;
	typeID = _typeID;
	
	code = new IntArray(MS.globalConfig.maxStructDefSize);
	tagAddress = new IntArray(MS.globalConfig.maxStructMembers);
	
	numMembers = 0;
	argsSize = -1; // set structSize after all arguments are set
	structSize = 0;
	codeTop = 0;

	//else
	{
		code[0] = makeInstruction(OP_STRUCT_DEF, 1, typeID);
		code[1] = 0; // empty name
		codeTop = 2;
	}
}

public StructDef (Semantics _semantics, int _nameID, int _typeID, int _size) 
{
	// primitive: no need to initialize arrays (code, tagAddress, nameOffset)
	semantics = _semantics;
	nameID = _nameID;
	typeID = _typeID;
	numMembers = -1;
	argsSize = -1; // set structSize after all arguments are set
	structSize = _size;
	codeTop = -1;
}

//

//	DESIGN
//			0: OP_STRUCT_MEMBER
//			1: nameID
//			2: address
//			3: memberSize = sizeOf
//	
//			0: OP_GENERIC_MEMBER
//			1: nameID
//			2: address
//			3: memberSize = sizeOf
//			4...n: argumentit
//	
//			0: OP_GENERIC_MEMBER: MS_GEN_TYPE_ARRAY
//			1: nameID
//			2: address
//			3: memberSize = sizeOf
//			4: item type
//			5: item count
//	
//			0: OP_GENERIC_MEMBER: MS_GEN_TYPE_CHARS
//			1: nameID
//			2: address
//			3: memberSize = sizeOf
//			4: char count

public int  addArray (int nameID, int itemType, int itemCount) 
{
	MS.assertion(itemCount > 0 && itemCount < MS.globalConfig.maxArraySize,MC.EC_INTERNAL, "invalid array size");
	StructDef sd = semantics.typeStructDefs[itemType];
	MS.assertion(sd != null,MC.EC_INTERNAL, "struct missing");
	int itemSize = sd.structSize;
	int dataSize = itemCount * itemSize; // "sizeof"
	
	// create tag
	
	tagAddress[numMembers] = codeTop;

	int memberTag = makeInstruction(OP_GENERIC_MEMBER, 5, MS_GEN_TYPE_ARRAY);
	code[codeTop] = memberTag;
	int address = structSize;
	code[codeTop + 1] = nameID;
	code[codeTop + 2] = address; // offset = sum of previous type sizes
	code[codeTop + 3] = dataSize; // "sizeof"
	
	code[codeTop + 4] = itemType;
	code[codeTop + 5] = itemCount;

	structSize += dataSize;
		
	codeTop += 6;

	numMembers++;
	
	return address;
}
public int  addChars (int nameID, int charCount) 
{
	int dataSize = (charCount/4) + 2; // "sizeof"
	
	// create tag
	
	tagAddress[numMembers] = codeTop;

	int memberTag = makeInstruction(OP_GENERIC_MEMBER, 4, MS_GEN_TYPE_CHARS);
	code[codeTop] = memberTag;
	int address = structSize;
	code[codeTop + 1] = nameID;
	code[codeTop + 2] = address; // offset = sum of previous type sizes
	code[codeTop + 3] = dataSize; // "sizeof"
	code[codeTop + 4] = charCount;

	structSize += dataSize;
		
	codeTop += 5;

	numMembers++;
	
	return address;
}

public int  addMember (int nameID, int type) 
{	
	// get size

	StructDef sd = semantics.typeStructDefs[type];
	MS.assertion(sd != null,MC.EC_INTERNAL, "struct missing");
	int memberSize = sd.structSize;
	return addMember(nameID, type, memberSize);
}

public int  addMember (int nameID, int type, int memberSize) 
{
	// create tag

	tagAddress[numMembers] = codeTop;

	int memberTag = makeInstruction(OP_STRUCT_MEMBER, 3, type);
	code[codeTop] = memberTag;
	int address = structSize;
	code[codeTop + 1] = nameID;
	code[codeTop + 2] = address; // offset = sum of previous type sizes
	code[codeTop + 3] = memberSize; // "sizeof"

	structSize += memberSize;
		
	codeTop += 4;

	numMembers++;
	
	return address;
}

public bool  hasMember  (MSText name)
{
	return getTagAddressByName(name) >= 0;
}

public int  getTagAddressByName (MSText name)
{
	int id = semantics.getTextID(name);
	if (id < 0) return -1;
	return getTagAddressByNameID(id);
}

public int  getTagAddressByNameID (int nameID)
{
	// get code offset for a member with a name
	
	for (int i=0; i<numMembers; i++)
	{
		int offset = tagAddress[i];
		if (code[offset + 1] == nameID) return offset;
	}
	return -1;
}

public int  getMemberTagByName (MSText name) 
{
	int index = getTagAddressByName(name);
	MS.assertion(index >= 0,MC.EC_INTERNAL, "undefined variable, ID: " + name);
	return code[index];
}
public int  getMemberAddressByName (MSText name) 
{
	int index = getTagAddressByName(name);
	MS.assertion(index >= 0,MC.EC_INTERNAL, "undefined variable, ID: " + name);
	return code[index+2]; // see above for definition
}
public int  getMemberSizeByName (MSText name) 
{
	int index = getTagAddressByName(name);
	MS.assertion(index >= 0,MC.EC_INTERNAL, "undefined variable, ID: " + name);
	return code[index+3]; // see above for definition
}

//////////////// GENERIC STUFF ////////////////

public int  getCharCount (MSText varName) 
{
	int index = getTagAddressByName(varName);
	MS.assertion(index >= 0,MC.EC_INTERNAL, "undefined variable: " + varName);
	int tag = code[index];
	MS.assertion(isCharsTag(tag), EC_SYNTAX, "not a chars: " + varName);
	return code[index + 4];
}
public int  getMemberCharCount (int index) 
{
	MS.assertion(indexInRange(index),MC.EC_INTERNAL, "argument index out of range: " + index);
	int tag = code[tagAddress[index]];
	if (isCharsTag(tag)) return -1;
	return code[index + 4];
}
public int  getMemberArrayItemType (MSText varName) 
{
	int index = getTagAddressByName(varName);
	MS.assertion(index >= 0,MC.EC_INTERNAL, "undefined variable: " + varName);
	int tag = code[index];
	MS.assertion(isArrayTag(tag), EC_SYNTAX, "not an array: " + varName);
	return code[index + 4]; // see above for definition
}
public int  getMemberArrayItemCount (MSText varName) 
{
	int index = getTagAddressByName(varName);
	MS.assertion(index >= 0,MC.EC_INTERNAL, "undefined variable: " + varName);
	int tag = code[index];
	MS.assertion(isArrayTag(tag), EC_SYNTAX, "not an array: " + varName);
	return code[index + 5]; // see above for definition
}
public int  getMemberArrayItemCountOrNegative (MSText varName) 
{
	int index = getTagAddressByName(varName);
	MS.assertion(index >= 0,MC.EC_INTERNAL, "undefined variable: " + varName);
	int tag = code[index];
	if (isArrayTag(tag)) return -1;
	return code[index + 5]; // see above for definition
}
public int  getMemberArrayItemCountOrNegative (int index) 
{
	MS.assertion(indexInRange(index),MC.EC_INTERNAL, "argument index out of range: " + index);
	int tag = code[tagAddress[index]];
	if (isArrayTag(tag)) return -1;
	return code[tagAddress[index] + 5]; // see above for definition
}

/////////////////////////////////////////////

public bool  indexInRange(int index)
{
	return index >= 0 && index < numMembers;
}
public int  getMemberTagByIndex (int index) 
{
	MS.assertion(indexInRange(index),MC.EC_INTERNAL, "argument index out of range: " + index);
	return code[tagAddress[index]];
}
public int  getMemberAddressByIndex (int index) 
{
	MS.assertion(indexInRange(index),MC.EC_INTERNAL, "argument index out of range: " + index);
	return code[tagAddress[index] + 1]; // see above for definition
}
public int  getMemberSizeByIndex (int index) 
{
	MS.assertion(indexInRange(index),MC.EC_INTERNAL, "argument index out of range: " + index);
	return code[tagAddress[index] + 2]; // see above for definition
}
public int  getMemberNameIDByIndex (int index) 
{
	// name ID of _n_th member
	
	int offset = tagAddress[index];
	return code[offset + 1];
	
	//MS.assertion(indexInRange(index),MC.EC_INTERNAL, "argument index out of range: " + index);
	//int offset = nameOffset[index];
	//return System.Text.Encoding.UTF8.GetString(MS.intsToBytes(code, offset + 2, code[offset + 1]));
}
public void print (Semantics sem) 
{
	MS.verbose(HORIZONTAL_LINE);
	MS.verbose("STRUCT CODE: " + sem.getText(nameID));
	MS.verbose(HORIZONTAL_LINE);
	if (numMembers == 0)
	{
		MS.verbose("        empty");
	}
	else
	{
		if (MS._verboseOn) printBytecode(code, codeTop, -1, true);
	}
	MS.verbose(HORIZONTAL_LINE);
}

}
}
