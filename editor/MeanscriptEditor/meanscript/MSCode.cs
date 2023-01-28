namespace Meanscript
{

	public class MSCode : MC
	{
		// interface for
		//  - compiling and running code
		//  - access data in MeanMachine

		MeanMachine mm;

		bool initialized;

		public MSStruct global;

		public MSCode()
		{
			mm = null;
			initialized = false;

			Reset();
		}
		public MSCode(MSInputStream input, int streamType)
		{
			MS.Assertion(streamType >= MS.globalConfig.STREAM_TYPE_FIRST && streamType <= MS.globalConfig.STREAM_TYPE_LAST, MC.EC_INTERNAL, "unknown stream type");

			mm = null;
			initialized = false;

			if (streamType == MS.globalConfig.STREAM_BYTECODE)
			{
				InitBytecode(input);
			}
			else if (streamType == MS.globalConfig.STREAM_SCRIPT)
			{
				mm = new MeanMachine(Compile(input));
			}
			else
			{
				MS.Assertion(false, MC.EC_INTERNAL, "unknown stream type");
			}
		}

		public MeanMachine GetMM()
		{
			return mm;
		}

		//;

		public void Reset()
		{
			mm = null;
			initialized = false;
		}

		public bool IsInitialized()
		{
			return initialized;
		}

		public void CheckInit()
		{
			MS.Assertion(initialized, MC.EC_INTERNAL, "MSCode is not initialized");
		}

		public void InitBytecode(MSInputStream input)
		{
			Reset();

			ByteCode byteCode = new ByteCode(input);
			mm = new MeanMachine(byteCode);

			initialized = true;
		}

		public void InitBytecode(ByteCode bc)
		{
			Reset();

			ByteCode byteCode = new ByteCode(bc);
			mm = new MeanMachine(byteCode);

			initialized = true;
		}
		/*
		public bool HasData(string name)
		{
			return mm.globals.HasData(name);
		}

		public bool HasArray(string name)
		{
			return mm.globals.HasArray(name);
		}

		public bool GetBool(string name)
		{
			CheckInit();
			return mm.globals.GetBool(name);
		}

		public int GetInt(string name)
		{
			CheckInit();
			return mm.globals.GetInt(name);
		}

		public long GetInt64(string name)
		{
			CheckInit();
			return mm.globals.GetInt64(name);
		}

		public float GetFloat(string name)
		{
			CheckInit();
			return mm.globals.GetFloat(name);
		}
		public double GetFloat64(string name)
		{
			CheckInit();
			return mm.globals.GetFloat64(name);
		}

		public string GetText(string name)
		{
			CheckInit();
			return mm.globals.GetText(name);
		}

		public string GetChars(string name)
		{
			CheckInit();
			return mm.globals.GetChars(name);
		}

		public string GetText(int textID)
		{
			CheckInit();
			return mm.globals.GetText(textID);
		}

		public MSData GetData(string name)
		{
			CheckInit();
			return mm.globals.GetMember(name);
		}

		public MSDataArray GetArray(string name)
		{
			CheckInit();
			return mm.globals.GetArray(name);
		}
		*/
		public void WriteReadOnlyData(MSOutputStream output)
		{
			mm.WriteReadOnlyData(output);
		}

		public void WriteCode(MSOutputStream output)
		{
			mm.WriteCode(output);
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
			if (initialized) mm.PrintGlobals();
			else MS.Print("printDetails: MSCode is not initialized");
		}

		public void DataOutputPrint(MSOutputPrint output)
		{
			mm.PrintGlobals();
			output.Close();
		}

		public ByteCode Compile(MSInputStream input)
		{
			Reset();

			TokenTree tree = Parser.Parse(input);
			Semantics semantics = new Semantics(tree);
			semantics.Analyze();
			ByteCode bc = Generator.Generate(tree, semantics);
			initialized = true;


			return bc;
		}

		public void Run()
		{
			MS.Assertion(mm != null, MC.EC_INTERNAL, "not initialized");
			mm.CallFunction(0);
		}

		public void Step()
		{
			MS.Assertion(false, MC.EC_INTERNAL, "TODO");
		}

		public void CompileAndRun(string s)
		{
			Reset();
			MSInputArray ia = new MSInputArray(s);
			ByteCode bc = Compile(ia);
			mm = new MeanMachine(bc);
			
			global = new MSStruct(mm, MC.GLOBALS_TYPE_ID, 1 ,0);

			Run();
		}
	}
}
