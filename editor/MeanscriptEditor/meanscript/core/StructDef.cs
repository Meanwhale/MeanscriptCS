using System;
using System.Collections.Generic;
using System.Text;

namespace Meanscript
{
	public class StructDef
	{
		private Types types;
		internal int nameID; // reference to Semantics' text

		internal MList<Member> members = new MList<Member>();

		private int offset = 0;

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

		public StructDef(Types _types, int _nameID)
		{
			types = _types;
			nameID = _nameID;
		}

		internal void PrintArgTypes(MSOutputPrint printOut)
		{
			foreach(var m in members)
				printOut.Print("<").Print(m.Ref.ToString()).Print(":").Print(m.Type.ToString()).Print(">");
			printOut.EndLine();
		}
		public override string ToString()
		{
			string s = "";
			foreach(var m in members)
			{
				if (m.NameID > 0) s += "<" + m.Ref.ToString() + ":" + types.texts.GetText(m.NameID) + ":" + m.Type.TypeNameString() + ">";
				else s += "<" + m.Ref.ToString() + ":" + m.Type.TypeNameString() + ">";
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
				if (it1.Value.Def.ID != it2.Value.Type.ID || it1.Value.Ref != it2.Value.Ref) return false;
			}
			return true;
		}
		public void AddMember(int nameID, ArgType at)
		{
			AddMember(nameID, at.Def, at.Ref);
		}
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
		internal void AddMember(int nameID, TypeDef typeDef, int refID, int address, int datasize, int index)
		{
			MS.Assertion(typeDef != null);
			members.AddLast(new Member(
				typeDef,			// data type
				(Arg)refID,			// reference type
				address,			// address
				datasize,			// "sizeof" the member
				index,
				nameID));			// index (0, 1, 2, ...)
			offset += datasize;
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
			int id = types.texts.GetTextID(name);
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
		
		internal void Info(MSOutputPrint o)
		{
			o.PrintLine(MC.HORIZONTAL_LINE);
			o.PrintLine("STRUCT CODE: " + types.texts.GetText(nameID));
			o.PrintLine(MC.HORIZONTAL_LINE);
			foreach(var m in members)
			{
				o.PrintLine("    " + m.Ref.ToString() + " : " + m.Type.TypeNameString() + " " + types.texts.GetText(m.NameID));
			}
			o.PrintLine(MC.HORIZONTAL_LINE);
		}

		internal void Encode(ByteCode bc, int typeID)
		{
			MS.Verbose("ENCODE StructDef: " + typeID + " " + types.texts.GetText(nameID));
			bc.AddInstructionWithData(MC.OP_STRUCT_DEF, 1, typeID, nameID);

			// encode members

			foreach(var m in members)
			{
				/*
				public readonly TypeDef Type;
				public readonly Arg Ref;
				public readonly int Address; // from 0
				public readonly int DataSize; // sizeof
				public readonly int Index; // 0, 1, 2, ...
				public readonly int NameID; // 0, 1, 2, ...
				*/

				bc.AddInstructionWithData(MC.OP_STRUCT_MEMBER, 6, typeID, m.NameID); // struct type and member name
				bc.AddWord(m.Type.ID);
				bc.AddWord((int)m.Ref);
				bc.AddWord(m.Address);
				bc.AddWord(m.DataSize);
				bc.AddWord(m.Index);
			}
		}

	}
}
