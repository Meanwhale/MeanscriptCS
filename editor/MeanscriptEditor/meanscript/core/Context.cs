namespace Meanscript
{

	public class Context : MC
	{
		internal int functionID;
		internal int returnType;
		internal int tagAddress;
		internal int codeStartAddress;
		internal int codeEndAddress;
		public StructDef variables;
		internal int numArgs; // number of arguments in the beginning of 'variables' struct
		internal MNode codeNode; // code block node where the function code is

		public Context(Semantics sem, int _nameID, int _functionID, int _returnType)
		{
			variables = new StructDef(sem, _nameID, _functionID);
			functionID = _functionID;
			returnType = _returnType;
			tagAddress = -1;

			codeNode = null;
			codeStartAddress = -1;
			codeEndAddress = -1;
		}
	}
}
