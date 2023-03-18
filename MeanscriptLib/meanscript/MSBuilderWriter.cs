
namespace Meanscript
{
	using Core;

	public class MSBuilderWriter
	{
		internal StructDef SD;
		internal MSBuilder builder;
		internal DynamicArray values = new DynamicArray(); // default values

		public MSBuilderWriter(MSBuilder _builder, StructDef _sd)
		{	
			builder = _builder;
			SD = _sd;
			if (SD.StructSize() > 0) values.Expand(SD.StructSize());
		}
		public void WriteInt(StructDef.Member member, int value)
		{
			MS.Assertion(member != null, MC.EC_DATA, "member is null");
			MS.Assertion(member.Type.ID == MC.BASIC_TYPE_INT, MC.EC_DATA, "not an int: " + builder.types.texts.GetText(member.NameID));
			values[member.Address] = value; // set initial value
		}

		internal StructDef.Member Get(string name)
		{
			int nameID = builder.types.texts.GetTextID(new MSText(name));
			MS.Assertion(nameID >= 0, MC.EC_DATA, "member not found: " + name);
			var m = SD.GetMemberByNameID(nameID);
			MS.Assertion(m != null, MC.EC_DATA, "member not found: " + name);
			return m;
		}
	}
}
