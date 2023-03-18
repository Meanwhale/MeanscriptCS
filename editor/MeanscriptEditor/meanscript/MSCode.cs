namespace Meanscript
{
	using Core;
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

		private MeanMachine mm = null;
		private Dictionary<int, MCNode> nodes;
		private bool initialized = false;

		public MSStruct global;

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
			global = new MSStruct(mm, MC.GLOBALS_TYPE_ID, 1 ,0);

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
			global = new MSStruct(mm, MC.GLOBALS_TYPE_ID, 1 ,0);
		}
	}
}
