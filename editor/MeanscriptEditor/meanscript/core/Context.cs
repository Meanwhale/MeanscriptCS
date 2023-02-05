using System;

namespace Meanscript
{

	public class Context
	{
		internal int functionID;
		internal TypeDef returnType;
		internal int tagAddress;
		internal int codeStartAddress;
		internal int codeEndAddress;
		public StructDef variables;
		internal int argsSize; // number of arguments in the beginning of 'variables' struct
		internal MNode codeNode; // code block node where the function code is

		public Context(Semantics sem, int _nameID, int _functionID, TypeDef _returnType)
		{
			variables = new StructDef(sem, _nameID);
			functionID = _functionID;
			returnType = _returnType;
			tagAddress = -1;

			codeNode = null;
			codeStartAddress = -1;
			codeEndAddress = -1;
		}

		internal void Info(MSOutputPrint o)
		{
			variables.Info(o);
		}
	}
}
