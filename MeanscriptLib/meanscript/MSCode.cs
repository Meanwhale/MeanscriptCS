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
		private Dictionary<int, MCNode> nodes;
		private MSStruct? _global;

		public MSStruct Global { get { if (_global == null) throw new MException(MC.EC_DATA ,"global is not initialized"); return _global; }}

		// root level data. for script it's MSStruct with global value.
		// for code generated data it can be any data type.
		public IDynamicObject? main;

		public MeanMachine MM { get { return mm; } }

		public MSCode(string s)
		{
			MSInputArray ia = new MSInputArray(s);
			var output = Compile(ia, out nodes);
			mm = new MeanMachine(new MSInputArray((MSOutputArray)output), nodes);
			
			InitMainObject();
		}
		public MSCode(MSScriptFileInput input)
		{
			var result = Compile(input, out nodes); // parse and compile script
			mm = new MeanMachine(new MSInputArray((MSOutputArray)result));
		}
		public MSCode(MSBytecodeFileInput input)
		{
			mm = InitBytecode(input);
		}
		public MSCode(MSInputArray input, StreamType st)
		{
			switch (st)
			{
				case StreamType.SCRIPT:
					var result = Compile(input, out nodes); // compi
					mm = new MeanMachine(new MSInputArray((MSOutputArray)result));
					break;
				case StreamType.BYTECODE:
					nodes = new ();
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
			nodes = new ();
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

		private MSOutput Compile(MSInput input, out Dictionary<int, MCNode> nodes)
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
					_global = new MSStruct(mm, typeID, 1, 0);
					return;
				}
				else
				{
					MS.Assertion(false);
				}
			}
		}
	}
}
