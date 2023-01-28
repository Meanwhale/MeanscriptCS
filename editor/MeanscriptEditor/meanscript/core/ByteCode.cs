using System;
using System.Collections.Generic;

namespace Meanscript
{

	public class ByteCode : MC
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

			// copy array

			for (int i = 0; i < codeTop; i++)
			{
				code[i] = bc.code[i];
			}
		}

		//;


		public void AddInstructionWithData(int operation, int size, int valueType, int data)
		{
			int instruction = MakeInstruction(operation, size, valueType);
			code[codeTop++] = instruction;
			AddWord(data);
			MS.Verbose("Add instruction with data: [" + GetOpName(instruction) + "] [" + data + "]");
		}

		public void AddInstruction(int operation, int size, int valueType)
		{
			int instruction = MakeInstruction(operation, size, valueType);
			MS.Verbose("Add instruction: [" + GetOpName(instruction) + "]");
			code[codeTop++] = instruction;
		}

		public void AddWord(int data)
		{
			//{if (MS.debug) {MS.print("data: " + data);}};
			code[codeTop++] = data;
		}

		public void WriteCode(MSOutputStream output)
		{
			for (int i = 0; i < codeTop; i++)
			{
				output.WriteInt(code[i]);
			}
		}

		public void WriteStructInit(MSOutputStream output)
		{
			int i = 0;

			// change init tag

			int op = (int)((code[i]) & OPERATION_MASK);
			MS.Assertion(op == OP_START_INIT, MC.EC_INTERNAL, "writeStructInit: bytecode error");
			output.WriteInt(MakeInstruction(OP_START_INIT, 1, BYTECODE_READ_ONLY));
			i++;
			output.WriteInt(code[i]);
			i++;

			int tagIndex = i;

			// copy necessary tags

			for (; i < codeTop; i++)
			{
				if (i == tagIndex)
				{
					tagIndex += InstrSize(code[i]) + 1;
					op = (int)((code[i]) & OPERATION_MASK);
					if (op == OP_FUNCTION)
					{
						// skip function inits
						i += InstrSize(code[i]);
						continue;
					}
					else if (op == OP_END_INIT)
					{
						return;
					}
				}
				output.WriteInt(code[i]);
			}
			MS.Assertion(false, MC.EC_INTERNAL, "bytecode init end tag not found");
		}
	}
}
