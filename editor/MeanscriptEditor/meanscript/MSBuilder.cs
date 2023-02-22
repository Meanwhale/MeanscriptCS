


namespace Meanscript
{
	using Core;

	// build bytecode data

	public class MSBuilder
	{
		
		private string packageName;
		internal CodeTypes types; // types and texts
		public MSStructBuilder globals;
		private MHeap heap = new MHeap();

		public MSBuilder(string _packageName)
		{
			packageName = _packageName;
			types = new CodeTypes(new Texts());
			globals = new MSStructBuilder(this, 0);
		}

		/*public void lockCheck()
		{
			// MS.Assertion(!structLock, MC.EC_INTERNAL, "Structs can't be defined after data is added");
		}

		public void addType(string typeName, StructDef sd)
		{
			lockCheck();
			int id = semantics.typeIDCounter++;
			sd.typeID = id;
			semantics.addStructDef(typeName, id, sd);
		}

		public void AddInt(string name, int value)
		{
			structLock = true;
			semantics.checkReserved(name);
			MS.Verbosen("BUILDER: New int: " + name + "\n");
			int address = variables.addMember(semantics, name, MS_TYPE_INT);
			values[address] = value;
		}

		public int createText(string value)
		{
			structLock = true;
			if (!(texts.ContainsKey(value)))
			{
				texts[value] = textIDCounter++;
			}
			return texts[value];
		}

		public void addText(string varName, string value)
		{
			structLock = true;
			semantics.checkReserved(varName);
			// add string to tree
			int textID = createText(value);
			int address = variables.addMember(semantics, varName, MS_TYPE_TEXT);
			values[address] = textID;
		}

		public int createStructDef(string name)
		{
			lockCheck();
			int id = semantics.typeIDCounter++;
			StructDef sd = new StructDef(name, id);
			semantics.addStructDef(name, id, sd);
			return id;
		}

		public void addMember(int structTypeID, string varName, int memberType)
		{
			StructDef sd = semantics.getType(structTypeID);
			sd.addMember(varName, memberType);
		}

		public void addArray(int typeID, string arrayName, int arraySize)
		{
			variables.addArray(semantics, arrayName, typeID, arraySize);
		}

		public MSWriter arrayItem(string arrayName, int arrayIndex)
		{
			int tag = variables.getMemberTag(arrayName);
			MS.Assertion((tag & OPERATION_MASK) == OP_ARRAY_MEMBER, MC.EC_INTERNAL, "not an array");
			int itemCount = variables.getMemberArrayItemCount(arrayName);
			MS.Assertion(arrayIndex >= 0 && arrayIndex < itemCount, MC.EC_INTERNAL, "index out of bounds: " + arrayIndex + " / " + itemCount);
			StructDef arrayItemType = semantics.getType((int)(tag & VALUE_TYPE_MASK));
			int itemSize = arrayItemType.structSize;
			int address = variables.getMemberAddress(arrayName);
			address += arrayIndex * itemSize;

			return new MSWriter(this, arrayItemType, address);
		}

		public MSWriter createStruct(string typeName, string varName)
		{
			StructDef sd = semantics.getType(typeName);
			return createStruct(sd.typeID, varName);
		}

		public MSWriter createStruct(int typeID, string varName)
		{
			structLock = true;
			semantics.checkReserved(varName);
			MeanCS.verbosen("BUILDER: New struct: ").print(varName).print("\n");
			StructDef sd = semantics.getType(typeID);
			int address = variables.addMember(semantics, varName, typeID);

			return new MSWriter(this, sd, address);
		}*/


		public void Generate(MSOutputArray output)
		{
			// TODO: generate bytecode

			// Semantics.WriteTypesAndGlobals(...)
			// write heap data: ks. GenerateDataCode. tee funktio.
			// myöhemmin specialit kuten mapin tall.
			
			
			// START_INIT and texts
			output.WriteInt(MC.MakeInstruction(MC.OP_START_INIT, 1, 0));
			output.WriteInt(types.texts.TextCount());

			// add texts
			foreach (var textEntry in types.texts.texts)
			{
				bc.codeTop = MC.AddTextInstruction(textEntry.Key, MC.OP_ADD_TEXT, bc.code, bc.codeTop, textEntry.Value);
			}

			Semantics.WriteTypesAndGlobals(bc, types, globals.SD);
			
			// kirjoita globaalit heapiin ID:llä 1

			MS.Assertion(globals.SD.StructSize() == globals.values.Count);
			heap.AllocGlobal(globals.SD.StructSize());
			heap.Write(DData.Role.GLOBAL, 1, MC.GLOBALS_TYPE_ID, globals.values.Data(), 0, globals.values.Count);

			// kirjoita koko heap kuten MM:ssa
			
			MeanMachine.WriteHeap(output, heap);

			bc.AddInstruction(MC.OP_END_DATA_INIT, 0, 0);
			bc.AddInstruction(MC.OP_END_INIT, 0, 0);

			return bc;
		}
	}
}
