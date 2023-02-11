namespace Meanscript
{
	using Core;

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
		private bool initialized = false;

		public MSStruct global;

		public MSCode(string s)
		{
			CompileAndRun(s);
		}
		public MSCode(MSInputStream input, StreamType st)
		{
			switch (st)
			{
				case StreamType.SCRIPT:
					mm = new MeanMachine(Compile(input));
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

		private void InitBytecode(MSInputStream input)
		{
			MS.Assertion(!initialized);	

			ByteCode byteCode = new ByteCode(input);
			mm = new MeanMachine(byteCode);
			global = new MSStruct(mm, MC.GLOBALS_TYPE_ID, 1 ,0);

			initialized = true;
		}

		public void InitBytecode(ByteCode bc)
		{
			ByteCode byteCode = new ByteCode(bc);
			mm = new MeanMachine(byteCode);

			initialized = true;
		}
		public void GenerateDataCode(MSOutputStream output)
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

		private ByteCode Compile(MSInputStream input)
		{
			TokenTree tree = Parser.Parse(input);
			Semantics semantics = new Semantics(tree);
			semantics.Analyze();
			ByteCode bc = Generator.Generate(tree, semantics);
			initialized = true;
			return bc;
		}

		private void Run()
		{
			MS.Assertion(mm != null, MC.EC_INTERNAL, "not initialized");
			mm.CallFunction(0);
		}

		public void Step()
		{
			MS.Assertion(false, MC.EC_INTERNAL, "TODO");
		}

		private void CompileAndRun(string s)
		{
			MSInputArray ia = new MSInputArray(s);
			ByteCode bc = Compile(ia);
			mm = new MeanMachine(bc);
			global = new MSStruct(mm, MC.GLOBALS_TYPE_ID, 1 ,0);

			Run();
		}
	}
}
