namespace Meanscript.Core
{

	public class MArgs
	{
		internal readonly int baseIndex; // stack base where struct data start from
		internal readonly CallbackType cb;

		public MArgs(CallbackType _cb, int _base)
		{
			cb = _cb;
			baseIndex = _base;
		}
	}
}
