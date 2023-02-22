


namespace Meanscript
{
	using Core;

	public class MSStructBuilder : MSWriter
	{	
		public MSStructBuilder(MSBuilder _builder, int nameID) : 
			base(_builder, new StructDef(_builder.types, nameID))
		{
		}

		public void AddInt(string name, int value = 0)
		{
			// value: default value for normal structs, initial value for global
			var member = SD.AddMember(builder.types.texts.AddText(name), new ArgType(Arg.DATA, builder.types.GetTypeDef(MC.BASIC_TYPE_INT)));
			WriteInt(member, value);
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
