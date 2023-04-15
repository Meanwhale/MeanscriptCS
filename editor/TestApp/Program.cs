namespace MeanscriptTest
{
	using Meanscript;
	public class MeanscriptTestApp
	{
		static void Main(string[] _)
		{
			MS.IsVerbose = false;

			// compile script

			var code = new MSCode("int a: 5");
			Console.WriteLine("script: a = " + code.Global["a"].Int());
		
			const string fileName = "meanscript.mb";

			// change value

			code.Global["a"].SetInt(6);

			// save bytecode

			var sw = new MSBytecodeFileOutput(fileName);
			code.MM.GenerateDataCode(sw);

			// read bytecode

			var input = new MSBytecodeFileInput(fileName);
			code = new MSCode(input);

			Console.WriteLine("bytecode: a = " + code.Global["a"].Int());
		}
	}
}