namespace Meanscript
{
	using Core;
	using System;
	using System.Collections.Generic;

	public class MSCode
	{
		// interface for
		//  - compiling and running code
		//  - access data in MeanMachine

		public enum StreamType
		{
			SCRIPT,
			BYTECODE
		}

		private MeanMachine mm;
		private Dictionary<int, MCNode>? nodes;
		public MSStruct? global;

		// root level data. for script it's MSStruct with global value.
		// for code generated data it can be any data type.
		public IDynamicObject? main;

		public MeanMachine MM { get { return mm; } }

		public MSCode(string s)
		{
			CompileAndRun(s);
		}
		public MSCode(MSInput input, StreamType st)
		{
			switch (st)
			{
				case StreamType.SCRIPT:
					var result = Compile(input);
					mm = new MeanMachine(new MSInputArray((MSOutputArray)result));
					break;
				case StreamType.BYTECODE:
					mm = InitBytecode(input);
					break;
				default:
					throw new MException();
			}
		}

		private MeanMachine InitBytecode(MSInput input)
		{
			mm = new MeanMachine(input);
			InitMainObject();
			return mm;
		}

		public void GenerateDataCode(MSOutput output)
		{
			mm.GenerateDataCode(output);
		}

		public void DataOutputPrint(MSOutputPrint output)
		{
			mm.PrintCurrentContext();
			output.Close();
		}

		private MSOutput Compile(MSInput input)
		{
			TokenTree tree = Parser.Parse(input);
			Semantics semantics = new Semantics(tree);
			semantics.Analyze();
			var output = new MSOutputArray();
			nodes = Generator.Generate(tree, semantics, output);
			return output;
		}

		public void Step()
		{
			MS.Assertion(false, MC.EC_INTERNAL, "TODO");
		}

		private void CompileAndRun(string s)
		{
			MSInputArray ia = new MSInputArray(s);
			var output = Compile(ia);
			mm = new MeanMachine(new MSInputArray((MSOutputArray)output), nodes);
			
			InitMainObject();
		}

		private void InitMainObject()
		{
			main = mm.Heap.GetDynamicObjectAt(1);
			
			// main data can be of any data type.
			// if it's struct, like for scripts, assign global.
			int typeID = MCHeap.TagType(main.tag);
			var mainType = mm.codeTypes.GetTypeDef(typeID);
			if (mainType is StructDefType)
			{
				if (main is MCStore store)
				{
					//public MSStruct(CodeTypes types, int typeID, IntArray dataCode, int offset, MCHeap heap)
					global = new MSStruct(mm, typeID, 1, 0);
				}
				else
				{
					MS.Assertion(false);
				}
			}
		}
	}
}
