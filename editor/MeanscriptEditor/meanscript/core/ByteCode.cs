using System.Collections.Generic;

namespace Meanscript.Core
{

	public class ByteCode
	{
		public int codeTop;
		public IntArray code;

		// save parsed nodes for debugging
		public Dictionary<int, MNode> nodes = new Dictionary<int, MNode>();

		public ByteCode()
		{
			code = new IntArray(MS.globalConfig.codeSize);
			codeTop = 0;
		}

		public ByteCode(MSInputStream input)
		{
			int byteCount = input.GetByteCount();
			MS.Assertion(byteCount % 4 == 0, MC.EC_INTERNAL, "bytecode file size not divisible by 4");
			int size = byteCount / 4;
			code = new IntArray(size);
			input.ReadArray(code, size);
			codeTop = size;
		}

		public ByteCode(ByteCode bc)
		{
			codeTop = bc.codeTop;
			code = new IntArray(codeTop);
			IntArray.Copy(bc.code, code, codeTop);
		}

		public void AddInstructionWithData(int operation, int size, int valueType, int data)
		{
			int instruction = MC.MakeInstruction(operation, size, valueType);
			code[codeTop++] = instruction;
			AddWord(data);
			MS.Verbose("add instruction with data: [" + MC.GetOpName(instruction) + "] [" + data + "]");
		}

		public void AddInstruction(int operation, int size, int valueType)
		{
			int instruction = MC.MakeInstruction(operation, size, valueType);
			MS.Verbose("add instruction: [" + MC.GetOpName(instruction) + "]");
			code[codeTop++] = instruction;
		}

		public void AddWord(int data)
		{
			//{if (MS.debug) {MS.print("data: " + data);}};
			code[codeTop++] = data;
		}
	}
}
