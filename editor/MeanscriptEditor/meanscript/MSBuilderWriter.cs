
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
		}
		public void WriteInt(StructDef.Member member, int value)
		{
			MS.Assertion(member != null, MC.EC_DATA, "member is null");
			MS.Assertion(member.Type.ID == MC.BASIC_TYPE_INT, MC.EC_DATA, "not an int: " + builder.types.texts.GetText(member.NameID));
			MS.Assertion(values.Count == member.Address, MC.EC_DATA, "values array error");
			values.Add(value); // set initial value
		}
	}
}
