namespace Meanscript
{
	using Core;
	using System;

	// build bytecode data

	public class MSBuilderMember
	{
		public DataTypeDef MemberType;
		public string MemberName;
		public int [] InitialValue;
		public StructDef.Member? SDMember = null;
		
		public MSBuilderMember(DataTypeDef memberType, string memberName, int[] initialValue)
		{
			MemberType = memberType;
			MemberName = memberName;
			InitialValue = initialValue;
		}
		public MSBuilderMember(DataTypeDef memberType, string memberName, IMSData init)
		{
			MS.Assertion(init.IsBuilderData());
			MemberType = memberType;
			MemberName = memberName;
			InitialValue = init.dataCode.Data();
		}
	}

	public class MSBuilder
	{
		
		private string packageName;
		internal CodeTypes types; // types and texts
		private MCHeap heap = new MCHeap();
		private IMSVar main;
		private bool globalSet = false;

		public MSBuilder(string _packageName)
		{
			packageName = _packageName;
			types = new CodeTypes(new Texts());
			//globals = new MSBuilderStructWriter(this, types.texts.AddText("global"), MC.GLOBALS_TYPE_ID);
		}

		public void CreateGlobal(IMSVar data)
		{
			MS.Assertion(!globalSet);
			main = data;
			globalSet = true;
		}

		public void CreateGlobalStruct(params MSBuilderMember [] members)
		{
			MS.Assertion(!globalSet);
			CreateStruct(null, MC.GLOBALS_TYPE_ID, members);
			globalSet = true;
		}

		public StructDefType CreateStruct(string name, params MSBuilderMember [] members)
		{
			return CreateStruct(name, -1, members);
		}

		private StructDefType CreateStruct(string name, int typeID, params MSBuilderMember [] members)
		{
			// create structdef type and return its type ID
			
			if (typeID < 0) typeID = types.GetNewTypeID();

			int nameID = 0;
			if (name != null)
			{
				var nameText = new MSText(name);
				MS.Assertion(types.IsNameValidAndAvailable(nameText, null, null));
				nameID = types.texts.AddText(nameText);
			}
			var sd = new StructDef(types, nameID);
			var sdType = new StructDefType(typeID, null, sd);
			types.AddTypeDef(sdType);

			// add members

			foreach(var m in members)
			{
				var memberNameText = new MSText(m.MemberName);
				MS.Assertion(types.IsNameValidAndAvailable(memberNameText, null, null));
				int memberNameID = types.texts.AddText(memberNameText);

				var sdMember = sd.AddMember(memberNameID, m.MemberType, Arg.DATA, m.InitialValue);
				m.SDMember = sdMember; // give it for later use
			}
			return sdType;
		}
		
		
		public MSBuilderMember IntMember(string name, int init, out MSBuilderMember member)
		{
			member = new MSBuilderMember(types.GetDataType(MC.BASIC_TYPE_INT), name, new int [] {
				init
			});
			return member;
		}
		public MSBuilderMember BoolMember(string name, bool init, out MSBuilderMember member)
		{
			member = new MSBuilderMember(types.GetDataType(MC.BASIC_TYPE_BOOL), name, new int [] {
				init ? 1 : 0
			});
			return member;
		}
		public MSBuilderMember Int64Member(string name, long init, out MSBuilderMember member)
		{
			member = new MSBuilderMember(types.GetDataType(MC.BASIC_TYPE_INT64), name, new int [] {
				MC.Int64highBits(init),
				MC.Int64lowBits(init)
			});
			return member;
		}
		public MSBuilderMember FloatMember(string name, float init, out MSBuilderMember member)
		{
			member = new MSBuilderMember(types.GetDataType(MC.BASIC_TYPE_FLOAT), name, new int [] {
				MS.FloatToIntFormat(init)
			});
			return member;
		}
		public MSBuilderMember Float64Member(string name, double init, out MSBuilderMember member)
		{
			long a = MS.Float64ToInt64Format(init);
			member = new MSBuilderMember(types.GetDataType(MC.BASIC_TYPE_FLOAT64), name, new int [] {
				MC.Int64highBits(a),
				MC.Int64lowBits(a)
			});
			return member;
		}
		public MSBuilderMember CharsMember(string name, int maxBytes, string init, out MSBuilderMember member)
		{
			// make and add chars type
			int size = (maxBytes / 4) + 2;
			var charsType = new GenericCharsType(types.GetNewTypeID(), maxBytes, size);
			types.AddTypeDef(charsType);
			var initText = new MSText(init);
			MS.Assertion(initText.NumBytes() <= maxBytes, MC.EC_DATA, "init string is too long");
			member = new MSBuilderMember(charsType, name, initText.GetData().Data());
			return member;
		}
		public MSBuilderMember TextMember(string name, string init, out MSBuilderMember member)
		{
			int initTextID = 0;
			if (!string.IsNullOrEmpty(init))
			{
				initTextID = types.texts.AddText(init);
			}
			member = new MSBuilderMember(types.GetDataType(MC.BASIC_TYPE_TEXT), name, new int [] {
				initTextID
			});
			return member;
		}


		internal MSArray NewArray(int itemTypeID, int itemCount)
		{
			// make and add array type
			var itemType = types.GetDataType(itemTypeID);
			var arrayType = new GenericArrayType(types, types.GetNewTypeID(), itemType, itemCount, types.GetNewTypeID());
			types.AddTypeDef(arrayType);

			// create MSArray

			return new MSArray(types, arrayType.ID, new IntArray(itemCount * itemType.SizeOf()), 0, null);
		}
		internal MSMap NewMap()
		{
			var map = heap.AllocMap(types);
			return map.map;
		}

		internal MSBuilderMember DataMember(string name, IMSData data, out MSBuilderMember member)
		{
			member = new MSBuilderMember(types.GetDataType(data.typeID), name, data);
			return member;
		}
		internal MSBuilderMember StructMember(string name, MSStruct str, out MSBuilderMember member)
		{
			member = new MSBuilderMember(str.structType, name, str);
			return member;
		}
		public MSStruct New(StructDefType sd)
		{
			return new MSStruct(types, sd.ID);
		}
		public MSData New(TypeDef dt)
		{
			MS.Assertion(dt is DataTypeDef);
			return new MSData(types, dt.ID);
		}
		internal MSBuilderMember ObjMember(string name, IMSData data, out MSBuilderMember member)
		{
			MS.Assertion(data.IsBuilderData());

			// write _str_ to heap, create an object type the data, and make its tag a member

			int tag = heap.AllocStoreObject(data.typeID, data.dataCode);
			var objectType = new ObjectType(types, types.GetNewTypeID(), types.GetDataType(data.typeID), types.GetNewTypeID());
			types.AddTypeDef(objectType);
			member = new MSBuilderMember(objectType, name, new int [] { tag });
			return member;
		}
		internal MSBuilderMember MapMember(string name, MSMap map, out MSBuilderMember member)
		{
			member = new MSBuilderMember(types.GetDataType(MC.BASIC_TYPE_MAP), name, new int [] {
				map.tag
			});
			return member;
		}

		public void Generate(MSOutputArray output)
		{
			// TODO: generate bytecode

			// Semantics.WriteTypesAndGlobals(...)
			// write heap data: ks. GenerateDataCode. tee funktio.
			// myöhemmin specialit kuten mapin tall.
			
			// START_INIT and texts
			output.WriteInt(MC.MakeInstruction(MC.OP_START_DEFINE, 0, 0));

			// add texts

			foreach (var textEntry in types.texts.texts)
			{
				MC.WriteTextInstruction(output, textEntry.Key, MC.OP_ADD_TEXT, textEntry.Value);
			}

			types.WriteTypes(output);
		
			// write globals

			if (main != null)
			{
				MS.Assertion(!heap.HasObject(1));
				if (main is MSData d)
				{
					heap.SetStoreObject(1, d.typeID, d.dataCode);
				}
				else if (main is MSMap m)
				{
					heap.SetMapObject(1, m);
				}
				else MS.Assertion(false, MC.EC_DATA, "unhandle main data type: " + main);
			}
			else
			{
				var globals = types.GetDataType(MC.GLOBALS_TYPE_ID);
				MS.Assertion(globals != null, MC.EC_DATA, "globals not set");
				MS.Assertion(globals is StructDefType);
				var sd = ((StructDefType)globals).SD;

				int [] tmp = new int [sd.StructSize()];
				int index = 0;

				// write global values to temp. array
				foreach(var m in sd.members)
				{
					if (m.Type is GenericCharsType)
					{
						MS.Assertion(m.InitData != null && m.InitData.Length <= m.DataSize);
					}
					else
					{
						MS.Assertion(m.InitData != null && m.InitData.Length == m.DataSize);
					}
					IntArray.Copy(m.InitData, 0, tmp, index, m.InitData.Length);
					index += m.DataSize;
				}

				heap.ReadFromArray(MCStore.Role.GLOBAL, 1, MC.GLOBALS_TYPE_ID, tmp, tmp.Length);
			}

			heap.WriteHeap(output);
			
			output.WriteOp(MC.OP_START_INIT, 0, 0);
			output.WriteOp(MC.OP_END_INIT, 0, 0);
		}
	}
}
