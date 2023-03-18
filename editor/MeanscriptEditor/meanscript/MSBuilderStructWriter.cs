


namespace Meanscript
{
	using Core;
	using System;

	public class MSBuilderStructWriter : MSBuilderWriter
	{
		private bool locked = false;
		public int TypeID;

		public MSBuilderStructWriter(MSBuilder _builder, int nameID, int typeID = -1) : 
			base(_builder, new StructDef(_builder.types, nameID))
		{
			// TODO: muutkin kuin global
			MS.Assertion(builder.types.texts.HasTextID(nameID));
			if (typeID < 0) typeID = builder.types.GetNewTypeID();
			builder.types.AddTypeDef(new StructDefType(typeID, null, SD));
			TypeID = typeID;
		}

		public StructDef.Member AddInt(string name, int value = 0)
		{
			CheckLock(); // Check that this is not lock
			// value: default value for normal structs, initial value for global
			var member = SD.AddMember(builder.types.texts.AddText(name), new ArgType(Arg.DATA, builder.types.GetTypeDef(MC.BASIC_TYPE_INT)));
			values.Add(0); // default value
			WriteInt(member, value); // set initial value
			return member;
		}

		internal MSBuilderWriter Add(MSBuilderStructWriter sw, string name)
		{
			CheckLock(); // Check that this is not lock
			sw.Lock(); //  Lock member's type 
			SD.AddMember(builder.types.texts.AddText(name), new ArgType(Arg.DATA, builder.types.GetDataType(sw.TypeID)));
			values.Expand(sw.SD.StructSize());
			return new MSBuilderWriter(builder, sw.SD);
		}
		public void CheckLock()
		{
			MS.Assertion(!locked, MC.EC_DATA, "MSBuilderStructWriter locked");
		}
		private void Lock()
		{
			locked = true;
		}

		/*public void setInt(string name, int value)
		{
			int memberAddress = sd.GetMemberAddress(name);
			MS.Assertion((sd.getMemberTag(name) & VALUE_TYPE_MASK) == MS_TYPE_INT, MC.EC_INTERNAL, "not an integer type: " + name);
			builder.values[baseAddress + memberAddress] = value;
		}

		public void setText(string name, string value)
		{
			int memberAddress = sd.getMemberAddress(name);
			MS.Assertion((sd.getMemberTag(name) & VALUE_TYPE_MASK) == MS_TYPE_TEXT, MC.EC_INTERNAL, "not a text type: " + name);
			int textID = builder.createText(value);
			builder.values[baseAddress + memberAddress] = textID;
		}*/

	}
}
