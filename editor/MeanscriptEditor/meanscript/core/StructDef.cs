using System;
using System.Collections.Generic;
using System.Text;

namespace Meanscript
{
	public class StructDef
	{
		private Semantics semantics;
		internal int nameID; // reference to Semantics' text

		MList<Member> members = new MList<Member>();

		private int offset = 0;
		public int ArgsSize = 0; // legacy? for functions

		// LEGACY:
		//internal IntArray code;
		//internal IntArray tagAddress; // tagAddress[n] = code (above) address of _n_th member
		//internal int numMembers;
		//internal int argsSize;
		//public int structSize;
		//internal int codeTop;

		public class Member
		{
			public readonly TypeDef Type;
			public readonly Arg Ref;
			public readonly int Address; // from 0
			public readonly int DataSize; // sizeof
			public readonly int Index; // 0, 1, 2, ...
			public readonly int NameID; // 0, 1, 2, ...

			public Member(TypeDef type, Arg r, int address, int dataSize, int index, int nameID)
			{
				Type = type;
				Ref = r;
				Address = address;
				DataSize = dataSize;
				Index = index;
				NameID = nameID;
			}
		}

		public StructDef(Semantics _semantics, int _nameID)
		{
			semantics = _semantics;
			nameID = _nameID;
		}

		internal void PrintArgTypes(MSOutputPrint printOut)
		{
			foreach(var m in members)
				printOut.Print("<").Print(m.Type.ToString()).Print(">");
			printOut.EndLine();
		}
		public override string ToString()
		{
			string s = "";
			foreach(var m in members)
			{
				if (m.NameID > 0) s += "<" + semantics.GetText(m.NameID) + ":" + m.Type + ">";
				else s += "<" + m.Type + ">";
			}
			return s;
		}

		internal bool Match(MList<ArgType> args)
		{
			if (args.Size() == 0 || args.Size() != members.Size()) return false;
			var it1 = args.Iterator();
			var it2 = members.Iterator();
			while(it1.Next())
			{
				it2.Next();
				// check that both data and reference types match
				if (it1.Value.Def.ID != it2.Value.Type.ID && it1.Value.Ref != it2.Value.Ref) return false;
			}
			return true;
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

		//public int AddArray(int nameID, int itemType, int itemCount)
		//{
		//	MS.Assertion(false);
		//	return 0;
		//	/*MS.Assertion(itemCount > 0 && itemCount < MS.globalConfig.maxArraySize, MC.EC_INTERNAL, "invalid array size");
		//	StructDef sd = semantics.typeStructDefs[itemType];
		//	MS.Assertion(sd != null, MC.EC_INTERNAL, "struct missing");
		//	int itemSize = sd.structSize;
		//	int dataSize = itemCount * itemSize; // "sizeof"

		//	// create tag

		//	tagAddress[numMembers] = codeTop;

		//	int memberTag = MakeInstruction(OP_GENERIC_MEMBER, 5, MS_GEN_TYPE_ARRAY);
		//	code[codeTop] = memberTag;
		//	int address = structSize;
		//	code[codeTop + 1] = nameID;
		//	code[codeTop + 2] = address; // offset = sum of previous type sizes
		//	code[codeTop + 3] = dataSize; // "sizeof"

		//	code[codeTop + 4] = itemType;
		//	code[codeTop + 5] = itemCount;

		//	structSize += dataSize;

		//	codeTop += 6;

		//	numMembers++;

		//	return address;*/
		//}
		//public int AddChars(int nameID, int charCount)
		//{
		//	MS.Assertion(false);
		//	return 0;
		//	/*
		//	int dataSize = (charCount / 4) + 2; // "sizeof"

		//	// create tag

		//	tagAddress[numMembers] = codeTop;

		//	int memberTag = MakeInstruction(OP_GENERIC_MEMBER, 4, MS_GEN_TYPE_CHARS);
		//	code[codeTop] = memberTag;
		//	int address = structSize;
		//	code[codeTop + 1] = nameID;
		//	code[codeTop + 2] = address; // offset = sum of previous type sizes
		//	code[codeTop + 3] = dataSize; // "sizeof"
		//	code[codeTop + 4] = charCount;

		//	structSize += dataSize;

		//	codeTop += 5;

		//	numMembers++;
		//	return address;
		//	*/
		//}

		//public int AddMember(int nameID, int type)
		//{
		//	MS.Assertion(false);
		//	return 0;
		//	/*
		//	// get size

		//	StructDef sd = semantics.typeStructDefs[type];
		//	MS.Assertion(sd != null, MC.EC_INTERNAL, "struct missing");
		//	int memberSize = sd.structSize;
		//	return AddMember(nameID, type, memberSize);
		//	*/
		//}

		//public int AddMember(int nameID, int type, int memberSize)
		//{
		//	// create tag

		//	tagAddress[numMembers] = codeTop;

		//	int memberTag = MakeInstruction(OP_STRUCT_MEMBER, 3, type);
		//	code[codeTop] = memberTag;
		//	int address = structSize;
		//	code[codeTop + 1] = nameID;
		//	code[codeTop + 2] = address; // offset = sum of previous type sizes
		//	code[codeTop + 3] = memberSize; // "sizeof"

		//	structSize += memberSize;

		//	codeTop += 4;

		//	numMembers++;

		//	return address;
		//}
		public void AddMember(int nameID, TypeDef type, Arg arg)
		{
			MS.Assertion(nameID < 0 || GetMemberByNameID(nameID) == null);
			int size;
			switch (arg)
			{
				case Arg.VOID: size = 0;
					break;
				case Arg.DATA: size = type.SizeOf();
					break;
				case Arg.ADDRESS: size = 1;
					break;
				default:
					MS.Assertion(false);
					size = 0;
					break;
			}
			members.AddLast(new Member(
				type,				// data type
				arg,				// reference type
				offset,				// address
				size,				// "sizeof" the member
				members.Size(),
				nameID));			// index (0, 1, 2, ...)
			offset += size;
		}

		internal void AddMember(ArgType arg)
		{
			AddMember(-1, arg.Def, arg.Ref);
		}

		public int StructSize()
		{
			return offset;
		}

		public int NumMembers()
		{
			return members.Size();
		}

		public Member GetMember(MSText name)
		{
			int id = semantics.GetTextID(name);
			return GetMemberByNameID(id);
		}

		public Member GetMemberByNameID(int nameID)
		{
			if (nameID < 0) return null;
			foreach(var m in members)
				if (nameID == m.NameID) return m;
			return null;
		}
		public Member GetMemberByIndex(int i)
		{
			foreach(var m in members)
				if (i == m.Index) return m;
			return null;
		}

		public bool HasMember(MSText name)
		{
			return GetMember(name) != null;
		}

		public bool HasMemberByNameID(int nameID)
		{
			return GetMemberByNameID(nameID) != null;
		}

		//public int GetTagAddressByNameID(int nameID)
		//{
		//	// get code offset for a member with a name

		//	for (int i = 0; i < numMembers; i++)
		//	{
		//		int offset = tagAddress[i];
		//		if (code[offset + 1] == nameID) return offset;
		//	}
		//	return -1;
		//}

		//public int GetMemberTagByName(MSText name)
		//{
		//	int index = GetTagAddressByName(name);
		//	MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable, ID: " + name);
		//	return code[index];
		//}
		//public int GetMemberAddressByName(MSText name)
		//{
		//	int index = GetTagAddressByName(name);
		//	MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable, ID: " + name);
		//	return code[index + 2]; // see above for definition
		//}
		//public int GetMemberSizeByName(MSText name)
		//{
		//	int index = GetTagAddressByName(name);
		//	MS.Assertion(index >= 0, MC.EC_INTERNAL, "undefined variable, ID: " + name);
		//	return code[index + 3]; // see above for definition
		//}

		//////////////// GENERIC STUFF ////////////////
		/*
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
		*/
		/////////////////////////////////////////////

		//public bool IndexInRange(int index)
		//{
		//	return index >= 0 && index < numMembers;
		//}
		//public int GetMemberTagByIndex(int index)
		//{
		//	MS.Assertion(IndexInRange(index), MC.EC_INTERNAL, "argument index out of range: " + index);
		//	return code[tagAddress[index]];
		//}
		//public int GetMemberAddressByIndex(int index)
		//{
		//	MS.Assertion(IndexInRange(index), MC.EC_INTERNAL, "argument index out of range: " + index);
		//	return code[tagAddress[index] + 1]; // see above for definition
		//}
		//public int GetMemberSizeByIndex(int index)
		//{
		//	MS.Assertion(IndexInRange(index), MC.EC_INTERNAL, "argument index out of range: " + index);
		//	return code[tagAddress[index] + 2]; // see above for definition
		//}
		//public int GetMemberNameIDByIndex(int index)
		//{
		//	// name ID of _n_th member
		//
		//	int offset = tagAddress[index];
		//	return code[offset + 1];
		//
		//	//MS.assertion(indexInRange(index),MC.EC_INTERNAL, "argument index out of range: " + index);
		//	//int offset = nameOffset[index];
		//	//return System.Text.Encoding.UTF8.GetString(MS.intsToBytes(code, offset + 2, code[offset + 1]));
		//}
		
		internal void Info(MSOutputPrint o)
		{
			o.PrintLine(MC.HORIZONTAL_LINE);
			o.PrintLine("STRUCT CODE: " + semantics.GetText(nameID));
			o.PrintLine(MC.HORIZONTAL_LINE);
			foreach(var m in members)
			{
				o.PrintLine("    " + m.Type.TypeNameString() + " " + semantics.GetText(m.NameID));
			}
			o.PrintLine(MC.HORIZONTAL_LINE);
		}

	}
}
