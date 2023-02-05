using System;
using System.Collections.Generic;

namespace Meanscript
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
			MS.Verbose("Add instruction with data: [" + MC.GetOpName(instruction) + "] [" + data + "]");
		}

		public void AddInstruction(int operation, int size, int valueType)
		{
			int instruction = MC.MakeInstruction(operation, size, valueType);
			MS.Verbose("Add instruction: [" + MC.GetOpName(instruction) + "]");
			code[codeTop++] = instruction;
		}

		public void AddWord(int data)
		{
			//{if (MS.debug) {MS.print("data: " + data);}};
			code[codeTop++] = data;
		}

		/*public void WriteStructInit(MSOutputStream output)
		{
			int i = 0;

			// change init tag

			int op = (int)((code[i]) & MC.OPERATION_MASK);
			MS.Assertion(op == MC.OP_START_INIT, MC.EC_INTERNAL, "writeStructInit: bytecode error");
			output.WriteInt(MC.MakeInstruction(MC.OP_START_INIT, 1, MC.BYTECODE_READ_ONLY));
			i++;
			output.WriteInt(code[i]);
			i++;

			int tagIndex = i;

			// copy necessary tags

			for (; i < codeTop; i++)
			{
				if (i == tagIndex)
				{
					tagIndex += MC.InstrSize(code[i]) + 1;
					op = (int)((code[i]) & MC.OPERATION_MASK);
					if (op == MC.OP_FUNCTION)
					{
						// skip function inits
						i += MC.InstrSize(code[i]);
						continue;
					}
					else if (op == MC.OP_END_INIT)
					{
						return;
					}
				}
				output.WriteInt(code[i]);
			}
			MS.Assertion(false, MC.EC_INTERNAL, "bytecode init end tag not found");
		}*/
	}
}
