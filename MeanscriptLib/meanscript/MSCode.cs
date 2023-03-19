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

		private MeanMachine? mm;
		private Dictionary<int, MCNode>? nodes;
		public MSStruct? global;

		// root level data. for script it's MSStruct with global value.
		// for code generated data it can be any data type.
		public MSData? main;

		private bool initialized = false;

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
					InitBytecode(input);
					break;
			}
		}

		public MeanMachine GetMM()
		{
			return mm;
		}

		private void InitBytecode(MSInput input)
		{
			MS.Assertion(!initialized);	
			mm = new MeanMachine(input);
			InitMainObject();
			initialized = true;
		}

		public void GenerateDataCode(MSOutput output)
		{
			mm.GenerateDataCode(output);
		}

		public void PrintCode()
		{
			if (initialized) mm.PrintCode();
			else MS.Print("printCode: MSCode is not initialized");
		}

		public void PrintDetails()
		{
			if (initialized) mm.PrintDetails();
			else MS.Print("printDetails: MSCode is not initialized");
		}

		public void PrintData()
		{
			if (initialized) mm.PrintCurrentContext();
			else MS.Print("printDetails: MSCode is not initialized");
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
			initialized = true;
			return output;
		}

		//private void Run()
		//{
		//	MS.Assertion(mm != null, MC.EC_INTERNAL, "not initialized");
		//	mm.CallFunction(0);
		//}

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
			var ddata = mm.Heap.GetStoreByIndex(1);
			//main = new MSData(mm.codeTypes, ddata.DataTypeID(), ddata.data, 0, mm.Heap);
			main = new MSData(mm, ddata.DataTypeID(), 1, 0);
			
			// main data can be of any data type.
			// if it's struct, like for scripts, assign global.

			var mainType = mm.codeTypes.GetTypeDef(main.typeID);
			if (mainType is StructDefType) global = main.Struct();
		}
	}
}
