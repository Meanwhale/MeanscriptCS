namespace Meanscript
{

	public class MCallback : MC
	{
		internal MS.MCallbackAction func;
		internal int returnType;
		internal StructDef argStruct;

		public MCallback(MS.MCallbackAction _func, int _returnType, StructDef _argStruct)
		{
			func = _func;
			returnType = _returnType;
			argStruct = _argStruct;
		}
	}
}
