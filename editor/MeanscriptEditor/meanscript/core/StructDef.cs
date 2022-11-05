namespace Meanscript
{

	public class StructDef : MC
	{
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

		public StructDef(Semantics _semantics, int _nameID, int _typeID)
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
				code[0] = MakeInstruction(OP_STRUCT_DEF, 1, typeID);
				code[1] = 0; // empty name
				codeTop = 2;
			}
		}

		public StructDef(Semantics _semantics, int _nameID, int _typeID, int _size)
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

		public int AddArray(int nameID, int itemType, int itemCount)
		{
			MS.Assertion(itemCount > 0 && itemCount < MS.globalConfig.maxArraySize, MC.EC_INTERNAL, "invalid array size");
			StructDef sd = semantics.typeStructDefs[itemType];
			MS.Assertion(sd != null, MC.EC_INTERNAL, "struct missing");
			int itemSize = sd.structSize;
			int dataSize = itemCount * itemSize; // "sizeof"

			// create tag

			tagAddress[numMembers] = codeTop;

			int memberTag = MakeInstruction(OP_GENERIC_MEMBER, 5, MS_GEN_TYPE_ARRAY);
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
		public int AddChars(int nameID, int charCount)
		{
			int dataSize = (charCount / 4) + 2; // "sizeof"

			// create tag

			tagAddress[numMembers] = codeTop;

			int memberTag = MakeInstruction(OP_GENERIC_MEMBER, 4, MS_GEN_TYPE_CHARS);
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

		public int AddMember(int nameID, int type)
		{
			// get size

			StructDef sd = semantics.typeStructDefs[type];
			MS.Assertion(sd != null, MC.EC_INTERNAL, "struct missing");
			int memberSize = sd.structSize;
			return AddMember(nameID, type, memberSize);
		}

		public int AddMember(int nameID, int type, int memberSize)
		{
			// create tag

			tagAddress[numMembers] = codeTop;

			int memberTag = MakeInstruction(OP_STRUCT_MEMBER, 3, type);
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

		public bool HasMember(MSText name)
		{
			return GetTagAddressByName(name) >= 0;
		}

		public int GetTagAddressByName(MSText name)
		{
			int id = semantics.GetTextID(name);
			if (id < 0) return -1;
			return GetTagAddressByNameID(id);
		}

		public int GetTagAddressByNameID(int nameID)
		{
			// get code offset for a member with a name

			for (int i = 0; i < numMembers; i++)
			{
				int offset = tagAddress[i];
				if (code[offset + 1] == nameID) return offset;
			}
			return -1;
		}

		public int GetMemberTagByName(MSText name)
		{
			int index = GetTagAddressByName(name);
			MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable, ID: " + name);
			return code[index];
		}
		public int GetMemberAddressByName(MSText name)
		{
			int index = GetTagAddressByName(name);
			MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable, ID: " + name);
			return code[index + 2]; // see above for definition
		}
		public int GetMemberSizeByName(MSText name)
		{
			int index = GetTagAddressByName(name);
			MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable, ID: " + name);
			return code[index + 3]; // see above for definition
		}

		//////////////// GENERIC STUFF ////////////////

		public int GetCharCount(MSText varName)
		{
			int index = GetTagAddressByName(varName);
			MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable: " + varName);
			int tag = code[index];
			MS.Assertion(IsCharsTag(tag), EC_SYNTAX, "not a chars: " + varName);
			return code[index + 4];
		}
		public int GetMemberCharCount(int index)
		{
			MS.Assertion(IndexInRange(index), MC.EC_INTERNAL, "argument index out of range: " + index);
			int tag = code[tagAddress[index]];
			if (IsCharsTag(tag)) return -1;
			return code[index + 4];
		}
		public int GetMemberArrayItemType(MSText varName)
		{
			int index = GetTagAddressByName(varName);
			MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable: " + varName);
			int tag = code[index];
			MS.Assertion(IsArrayTag(tag), EC_SYNTAX, "not an array: " + varName);
			return code[index + 4]; // see above for definition
		}
		public int GetMemberArrayItemCount(MSText varName)
		{
			int index = GetTagAddressByName(varName);
			MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable: " + varName);
			int tag = code[index];
			MS.Assertion(IsArrayTag(tag), EC_SYNTAX, "not an array: " + varName);
			return code[index + 5]; // see above for definition
		}
		public int GetMemberArrayItemCountOrNegative(MSText varName)
		{
			int index = GetTagAddressByName(varName);
			MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable: " + varName);
			int tag = code[index];
			if (IsArrayTag(tag)) return -1;
			return code[index + 5]; // see above for definition
		}
		public int GetMemberArrayItemCountOrNegative(int index)
		{
			MS.Assertion(IndexInRange(index), MC.EC_INTERNAL, "argument index out of range: " + index);
			int tag = code[tagAddress[index]];
			if (IsArrayTag(tag)) return -1;
			return code[tagAddress[index] + 5]; // see above for definition
		}

		/////////////////////////////////////////////

		public bool IndexInRange(int index)
		{
			return index >= 0 && index < numMembers;
		}
		public int GetMemberTagByIndex(int index)
		{
			MS.Assertion(IndexInRange(index), MC.EC_INTERNAL, "argument index out of range: " + index);
			return code[tagAddress[index]];
		}
		public int GetMemberAddressByIndex(int index)
		{
			MS.Assertion(IndexInRange(index), MC.EC_INTERNAL, "argument index out of range: " + index);
			return code[tagAddress[index] + 1]; // see above for definition
		}
		public int GetMemberSizeByIndex(int index)
		{
			MS.Assertion(IndexInRange(index), MC.EC_INTERNAL, "argument index out of range: " + index);
			return code[tagAddress[index] + 2]; // see above for definition
		}
		public int GetMemberNameIDByIndex(int index)
		{
			// name ID of _n_th member

			int offset = tagAddress[index];
			return code[offset + 1];

			//MS.assertion(indexInRange(index),MC.EC_INTERNAL, "argument index out of range: " + index);
			//int offset = nameOffset[index];
			//return System.Text.Encoding.UTF8.GetString(MS.intsToBytes(code, offset + 2, code[offset + 1]));
		}
		public void Print(Semantics sem)
		{
			MS.Verbose(HORIZONTAL_LINE);
			MS.Verbose("STRUCT CODE: " + sem.GetText(nameID));
			MS.Verbose(HORIZONTAL_LINE);
			if (numMembers == 0)
			{
				MS.Verbose("        empty");
			}
			else
			{
				if (MS._verboseOn) PrintBytecode(code, codeTop, -1, true);
			}
			MS.Verbose(HORIZONTAL_LINE);
		}

	}
}
